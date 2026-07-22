// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.ClientThemeEditor;

internal static class JsonFiles
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private const long MaxJsonBytes = 4 * 1024 * 1024;

    public static T Read<T>(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var length = new FileInfo(fullPath).Length;
        if (length <= 0 || length > MaxJsonBytes)
        {
            throw new InvalidDataException($"JSON file '{fullPath}' must be between 1 byte and {MaxJsonBytes} bytes.");
        }

        var json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new InvalidDataException($"JSON file '{fullPath}' is empty or invalid.");
    }

    public static void Write<T>(string path, T value)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var json = JsonSerializer.Serialize(value, Options) + Environment.NewLine;
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
        if (bytes.LongLength > MaxJsonBytes)
        {
            throw new InvalidDataException($"Serialized JSON exceeds the {MaxJsonBytes}-byte limit.");
        }

        var temporary = fullPath + $".tmp.{Guid.NewGuid():N}";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}

internal static class HexCodec
{
    public static byte[] ParseExact(string value, string fieldName)
    {
        var tokens = Tokenize(value);
        if (tokens.Length == 0)
        {
            throw new InvalidDataException($"{fieldName} cannot be empty.");
        }

        var bytes = new byte[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] is "?" or "??")
            {
                throw new InvalidDataException($"{fieldName} cannot contain wildcards.");
            }

            if (!byte.TryParse(tokens[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out bytes[i]))
            {
                throw new InvalidDataException($"{fieldName} contains invalid byte '{tokens[i]}'.");
            }
        }

        return bytes;
    }

    public static byte?[] ParsePattern(string value)
    {
        var tokens = Tokenize(value);
        if (tokens.Length == 0 || tokens.Length > 1024)
        {
            throw new InvalidDataException("Pattern must contain between 1 and 1024 bytes.");
        }

        var pattern = new byte?[tokens.Length];
        var concrete = 0;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] is "?" or "??")
            {
                pattern[i] = null;
                continue;
            }

            if (!byte.TryParse(tokens[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidDataException($"Pattern contains invalid byte '{tokens[i]}'.");
            }

            pattern[i] = parsed;
            concrete++;
        }

        if (concrete == 0)
        {
            throw new InvalidDataException("Pattern cannot consist entirely of wildcards.");
        }

        return pattern;
    }

    public static byte[] EncodeColor(string color, string encoding)
    {
        var normalized = color.Trim().TrimStart('#');
        if (normalized.Length == 6)
        {
            normalized += "FF";
        }

        if (normalized.Length != 8 ||
            !uint.TryParse(normalized, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidDataException($"Color '{color}' must be #RRGGBB or #RRGGBBAA.");
        }

        var rgba = Enumerable.Range(0, 4)
            .Select(index => byte.Parse(normalized.Substring(index * 2, 2),
                NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture))
            .ToArray();

        return encoding.Trim().ToUpperInvariant() switch
        {
            "RGBA" => [rgba[0], rgba[1], rgba[2], rgba[3]],
            "BGRA" => [rgba[2], rgba[1], rgba[0], rgba[3]],
            "ARGB" => [rgba[3], rgba[0], rgba[1], rgba[2]],
            "ABGR" => [rgba[3], rgba[2], rgba[1], rgba[0]],
            "RGB" => [rgba[0], rgba[1], rgba[2]],
            "BGR" => [rgba[2], rgba[1], rgba[0]],
            _ => throw new InvalidDataException($"Unsupported color encoding '{encoding}'.")
        };
    }

    public static string Format(IEnumerable<byte> bytes)
        => string.Join(' ', bytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));

    private static string[] Tokenize(string value)
        => value.Split(new[] { ' ', '\t', '\r', '\n', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
}

internal static class PatternMatcher
{
    public static IReadOnlyList<int> FindAll(
        ReadOnlySpan<byte> content,
        IReadOnlyList<byte?> pattern,
        int stopAfter)
    {
        if (pattern.Count == 0 || pattern.Count > content.Length || stopAfter < 1)
        {
            return Array.Empty<int>();
        }

        var anchorIndex = Enumerable.Range(0, pattern.Count).First(index => pattern[index].HasValue);
        var anchor = pattern[anchorIndex]!.Value;
        var matches = new List<int>();
        for (var offset = 0; offset <= content.Length - pattern.Count; offset++)
        {
            if (content[offset + anchorIndex] != anchor)
            {
                continue;
            }

            var found = true;
            for (var index = 0; index < pattern.Count; index++)
            {
                var expected = pattern[index];
                if (expected.HasValue && content[offset + index] != expected.Value)
                {
                    found = false;
                    break;
                }
            }

            if (!found)
            {
                continue;
            }

            matches.Add(offset);
            if (matches.Count >= stopAfter)
            {
                break;
            }
        }

        return matches;
    }
}

internal static class PeInspector
{
    private const long MaxClientBytes = 1024L * 1024 * 1024;

    public static ClientIdentity Inspect(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Client executable was not found.", fullPath);
        }

        var info = new FileInfo(fullPath);
        if (info.Length < 128 || info.Length > MaxClientBytes)
        {
            throw new InvalidDataException(
                $"Input must be between 128 bytes and {MaxClientBytes} bytes.");
        }

        using var stream = File.OpenRead(fullPath);
        using var reader = new BinaryReader(stream);
        if (reader.ReadUInt16() != 0x5A4D)
        {
            throw new InvalidDataException("Input does not contain an MZ header.");
        }

        stream.Position = 0x3C;
        var peOffset = reader.ReadInt32();
        if (peOffset < 0x40 || peOffset > info.Length - 6)
        {
            throw new InvalidDataException("PE header offset is outside the file.");
        }

        stream.Position = peOffset;
        if (reader.ReadUInt32() != 0x00004550)
        {
            throw new InvalidDataException("Input does not contain a PE signature.");
        }

        var machine = reader.ReadUInt16();
        var architecture = machine switch
        {
            0x014c => "x86",
            0x8664 => "x64",
            0x01c4 => "arm",
            0xaa64 => "arm64",
            _ => $"unknown-0x{machine:X4}"
        };

        stream.Position = 0;
        var sha256 = Convert.ToHexString(SHA256.HashData(stream));
        var version = FileVersionInfo.GetVersionInfo(fullPath).FileVersion ?? string.Empty;
        return new ClientIdentity(info.Name, architecture, version, info.Length, sha256);
    }
}
