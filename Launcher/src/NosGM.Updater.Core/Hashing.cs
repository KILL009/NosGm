// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Security.Cryptography;

namespace NosGM.Updater.Core;

public static class Hashing
{
    public static bool IsSha256(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length == 64 &&
           value.All(Uri.IsHexDigit);

    public static async Task<string> Sha256FileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            useAsync: true);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    public static async Task<(long Length, string Sha256)> CopyBoundedAndHashAsync(
        Stream source,
        string destinationPath,
        long expectedSize,
        Action<long>? onBytesWritten,
        CancellationToken cancellationToken)
    {
        if (expectedSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedSize));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 128 * 1024,
            useAsync: true);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
        long total = 0;
        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                total = checked(total + read);
                if (total > expectedSize)
                {
                    throw new InvalidDataException(
                        $"Download exceeded its declared size of {expectedSize} bytes.");
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                onBytesWritten?.Invoke(read);
            }

            await destination.FlushAsync(cancellationToken);
            destination.Flush(flushToDisk: true);

            if (total != expectedSize)
            {
                throw new InvalidDataException(
                    $"Download length {total} does not match declared size {expectedSize}.");
            }

            return (total, Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
