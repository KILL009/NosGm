// SPDX-License-Identifier: BSL-1.0
// Archive layout and text decoding behavior adapted from OnexExplorer.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace NosGM.ResourceExplorer;

internal static class ArchiveReader
{
    private const int MaxEntries = 250_000;
    private const int MaxNameBytes = 4096;
    private const int MaxEntryBytes = 512 * 1024 * 1024;

    public static ArchiveDocument Read(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fileLength = new FileInfo(fullPath).Length;
        if (fileLength > int.MaxValue)
        {
            throw new InvalidDataException("Archives larger than 2 GiB are not supported by this read-only release.");
        }
        var bytes = File.ReadAllBytes(fullPath);
        if (bytes.Length < 4)
        {
            throw new InvalidDataException("The input is too small to be a supported archive.");
        }

        var sha = Hash(bytes);
        return IsZlibArchive(bytes)
            ? ReadZlib(fullPath, bytes, sha)
            : ReadText(fullPath, bytes, sha);
    }

    private static bool IsZlibArchive(byte[] bytes)
    {
        var prefix = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 16));
        return prefix.StartsWith("NT Data", StringComparison.Ordinal)
            || prefix.StartsWith("32GBS V1.0", StringComparison.Ordinal)
            || prefix.StartsWith("ITEMS V1.0", StringComparison.Ordinal);
    }

    private static ArchiveDocument ReadZlib(string path, byte[] bytes, string sha)
    {
        if (bytes.Length < 21)
        {
            throw new InvalidDataException("The compressed archive header is incomplete.");
        }

        var reader = new SafeBinaryReader(bytes);
        var rawHeader = reader.ReadBytes(16, "archive header");
        var header = CleanHeader(rawHeader);
        var count = reader.ReadInt32("entry count");
        ValidateCount(count);
        _ = reader.ReadByte("separator byte");

        var indexSize = checked(count * 8);
        if (reader.Remaining < indexSize)
        {
            throw new InvalidDataException("The archive index is truncated.");
        }

        var index = new List<(int Id, int Offset)>(count);
        for (var i = 0; i < count; i++)
        {
            index.Add((reader.ReadInt32($"entry {i} id"), reader.ReadInt32($"entry {i} offset")));
        }

        var minimumDataOffset = reader.Position;
        var document = NewDocument(path, bytes, sha, ArchiveFormat.NosZlib, header);
        var duplicateNames = new Dictionary<int, int>();

        for (var i = 0; i < index.Count; i++)
        {
            var (id, offset) = index[i];
            if (offset < minimumDataOffset || offset > bytes.Length - 13)
            {
                throw new InvalidDataException($"Entry {i} has an invalid data offset: {offset}.");
            }

            reader.Seek(offset, $"entry {i} offset");
            _ = reader.ReadInt32($"entry {i} creation date");
            var uncompressedSize = reader.ReadInt32($"entry {i} uncompressed size");
            var storedSize = reader.ReadInt32($"entry {i} stored size");
            var compressed = reader.ReadByte($"entry {i} compression flag") != 0;
            ValidateSize(uncompressedSize, $"entry {i} uncompressed size");
            ValidateSize(storedSize, $"entry {i} stored size");
            var stored = reader.ReadBytes(storedSize, $"entry {i} content");
            var content = compressed ? Decompress(stored, uncompressedSize, i) : stored;

            if (!compressed && uncompressedSize != storedSize)
            {
                throw new InvalidDataException($"Entry {i} is uncompressed but declares {uncompressedSize} bytes and stores {storedSize}.");
            }

            duplicateNames.TryGetValue(id, out var duplicate);
            duplicateNames[id] = duplicate + 1;
            var name = duplicate == 0 ? id.ToString() : $"{id}_{duplicate + 1}";
            document.Entries.Add(new ArchiveEntry
            {
                Index = i,
                Id = id,
                Name = name,
                Offset = offset,
                StoredSize = storedSize,
                UncompressedSize = content.Length,
                IsCompressed = compressed,
                Content = content,
                Sha256 = Hash(content)
            });
        }

        return document;
    }

    private static ArchiveDocument ReadText(string path, byte[] bytes, string sha)
    {
        var reader = new SafeBinaryReader(bytes);
        var count = reader.ReadInt32("text entry count");
        ValidateCount(count);
        if (count == 0)
        {
            throw new InvalidDataException("A headerless file with zero entries is not accepted as a text archive.");
        }

        var document = NewDocument(path, bytes, sha, ArchiveFormat.NosText, "headerless-text-container");
        for (var i = 0; i < count; i++)
        {
            var entryOffset = reader.Position;
            var fileNumber = reader.ReadInt32($"text entry {i} number");
            var nameSize = reader.ReadInt32($"text entry {i} name size");
            if (nameSize <= 0 || nameSize > MaxNameBytes)
            {
                throw new InvalidDataException($"Text entry {i} has an invalid name size: {nameSize}.");
            }

            var nameBytes = reader.ReadBytes(nameSize, $"text entry {i} name");
            var name = DecodeName(nameBytes);
            if (!IsReasonableName(name))
            {
                throw new InvalidDataException($"Text entry {i} has an unsafe or implausible name.");
            }

            var isDat = reader.ReadInt32($"text entry {i} DAT flag") != 0;
            var storedSize = reader.ReadInt32($"text entry {i} stored size");
            ValidateSize(storedSize, $"text entry {i} stored size");
            var stored = reader.ReadBytes(storedSize, $"text entry {i} content");
            var content = isDat || name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
                ? TextDecryptors.DecryptDat(stored, MaxEntryBytes)
                : TextDecryptors.DecryptList(stored, MaxEntryBytes);

            document.Entries.Add(new ArchiveEntry
            {
                Index = i,
                Id = fileNumber,
                Name = name,
                Offset = entryOffset,
                StoredSize = storedSize,
                UncompressedSize = content.Length,
                IsCompressed = isDat,
                Content = content,
                Sha256 = Hash(content),
                EncodingHint = EncodingHints.ForFileName(name)
            });
        }

        if (reader.Remaining is not 0 and not 12)
        {
            document.Diagnostics.Add(new Diagnostic("warning", "UNEXPECTED_TRAILING_BYTES", $"The text archive contains {reader.Remaining} trailing bytes."));
        }

        return document;
    }

    private static byte[] Decompress(byte[] stored, int expectedSize, int entryIndex)
    {
        using var input = new MemoryStream(stored, writable: false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: false);
        using var output = new MemoryStream(Math.Min(expectedSize, 16 * 1024 * 1024));
        var buffer = new byte[81920];
        while (true)
        {
            var read = zlib.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }
            if (output.Length + read > MaxEntryBytes || output.Length + read > expectedSize)
            {
                throw new InvalidDataException($"Entry {entryIndex} decompression exceeds its declared or allowed size.");
            }
            output.Write(buffer, 0, read);
        }

        if (output.Length != expectedSize)
        {
            throw new InvalidDataException($"Entry {entryIndex} decompressed to {output.Length} bytes, expected {expectedSize}.");
        }
        return output.ToArray();
    }

    private static ArchiveDocument NewDocument(string path, byte[] bytes, string sha, ArchiveFormat format, string header) => new()
    {
        InputPath = path,
        InputSha256 = sha,
        InputSize = bytes.LongLength,
        Format = format,
        Header = header
    };

    private static string CleanHeader(byte[] bytes) => Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ');

    private static string DecodeName(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static bool IsReasonableName(string name) => name.Length is > 0 and <= 512 && name.All(c => !char.IsControl(c) && c != '\0');

    private static void ValidateCount(int count)
    {
        if (count < 0 || count > MaxEntries)
        {
            throw new InvalidDataException($"Archive entry count {count} is outside the allowed range 0..{MaxEntries}.");
        }
    }

    private static void ValidateSize(int size, string field)
    {
        if (size < 0 || size > MaxEntryBytes)
        {
            throw new InvalidDataException($"{field} {size} is outside the allowed range 0..{MaxEntryBytes}.");
        }
    }

    public static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
