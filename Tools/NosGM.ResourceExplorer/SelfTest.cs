// SPDX-License-Identifier: BSL-1.0

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace NosGM.ResourceExplorer;

internal static class SelfTest
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "nosgm-resource-explorer-self-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var zlibPath = Path.Combine(root, "synthetic.NOS");
            File.WriteAllBytes(zlibPath, BuildZlibArchive());
            var zlib = ArchiveReader.Read(zlibPath);
            Require(zlib.Format == ArchiveFormat.NosZlib, "zlib format");
            Require(zlib.Entries.Count == 2, "zlib count");
            Require(Encoding.UTF8.GetString(zlib.Entries[0].Content) == "alpha", "compressed payload");
            Require(Encoding.UTF8.GetString(zlib.Entries[1].Content) == "beta", "plain payload");

            var output = Path.Combine(root, "extract");
            foreach (var entry in zlib.Entries)
            {
                File.WriteAllBytes(ExtractionSandbox.GetSafePath(output, entry), entry.Content);
            }
            Require(Directory.GetFiles(output).Length == 2, "safe extraction");

            var textPath = Path.Combine(root, "text.NOS");
            File.WriteAllBytes(textPath, BuildTextArchive());
            var text = ArchiveReader.Read(textPath);
            Require(text.Format == ArchiveFormat.NosText, "text format");
            Require(text.Entries.Count == 1, "text count");
            Require(Encoding.ASCII.GetString(text.Entries[0].Content) == "hello\n", "list decoding");

            var traversal = new ArchiveEntry { Index = 0, Name = "../../escape.txt", Offset = 0, StoredSize = 1, UncompressedSize = 1, IsCompressed = false, Content = [1], Sha256 = "x" };
            var safe = ExtractionSandbox.GetSafePath(output, traversal);
            Require(Path.GetDirectoryName(safe) == Path.GetFullPath(output), "path sandbox");

            Console.WriteLine("NosGM.ResourceExplorer self-test passed.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] BuildZlibArchive()
    {
        var first = Encoding.UTF8.GetBytes("alpha");
        var second = Encoding.UTF8.GetBytes("beta");
        var compressed = Compress(first);
        const int count = 2;
        var dataStart = 16 + 4 + 1 + count * 8;
        var firstOffset = dataStart;
        var secondOffset = firstOffset + 13 + compressed.Length;
        using var stream = new MemoryStream();
        var header = new byte[16];
        Encoding.ASCII.GetBytes("NT Data 24").CopyTo(header, 0);
        stream.Write(header);
        WriteInt(stream, count);
        stream.WriteByte(0);
        WriteInt(stream, 100);
        WriteInt(stream, firstOffset);
        WriteInt(stream, 101);
        WriteInt(stream, secondOffset);
        WriteEntry(stream, first, compressed, true);
        WriteEntry(stream, second, second, false);
        return stream.ToArray();
    }

    private static byte[] BuildTextArchive()
    {
        var line = Encoding.ASCII.GetBytes("hello");
        using var encoded = new MemoryStream();
        WriteInt(encoded, 1);
        WriteInt(encoded, line.Length);
        foreach (var value in line) encoded.WriteByte((byte)(value ^ 1));
        var payload = encoded.ToArray();
        var name = Encoding.ASCII.GetBytes("_code_en_test.lst");
        using var stream = new MemoryStream();
        WriteInt(stream, 1);
        WriteInt(stream, 7);
        WriteInt(stream, name.Length);
        stream.Write(name);
        WriteInt(stream, 0);
        WriteInt(stream, payload.Length);
        stream.Write(payload);
        return stream.ToArray();
    }

    private static byte[] Compress(byte[] value)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) zlib.Write(value);
        return output.ToArray();
    }

    private static void WriteEntry(Stream stream, byte[] uncompressed, byte[] stored, bool compressed)
    {
        WriteInt(stream, 0);
        WriteInt(stream, uncompressed.Length);
        WriteInt(stream, stored.Length);
        stream.WriteByte(compressed ? (byte)1 : (byte)0);
        stream.Write(stored);
    }

    private static void WriteInt(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"Self-test failed: {name}.");
    }
}
