// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.Updater.Core;

public static class JsonSupport
{
    public const long MaxJsonBytes = 4 * 1024 * 1024;

    public static JsonSerializerOptions CreateOptions(bool writeIndented = true)
        => new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

    public static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists || info.Length <= 0 || info.Length > MaxJsonBytes)
        {
            throw new InvalidDataException($"JSON file '{fullPath}' must exist and contain between 1 and {MaxJsonBytes} bytes.");
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return await JsonSerializer.DeserializeAsync<T>(stream, CreateOptions(), cancellationToken)
            ?? throw new InvalidDataException($"JSON file '{fullPath}' is empty or invalid.");
    }

    public static async Task WriteAtomicAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var json = JsonSerializer.Serialize(value, CreateOptions()) + Environment.NewLine;
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
        if (bytes.LongLength > MaxJsonBytes)
        {
            throw new InvalidDataException($"Serialized JSON exceeds the {MaxJsonBytes}-byte limit.");
        }

        var temporary = fullPath + $".tmp.{Guid.NewGuid():N}";
        try
        {
            await using (var stream = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
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
