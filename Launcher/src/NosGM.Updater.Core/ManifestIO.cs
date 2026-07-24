// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace NosGM.Updater.Core;

public static class ManifestIO
{
    public static async Task<ReleaseManifest> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var manifest = await JsonSupport.ReadAsync<ReleaseManifest>(path, cancellationToken);
        ManifestValidator.Validate(manifest, requireSignature: true);
        return manifest;
    }

    public static ReleaseManifest ReadUtf8(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.Length <= 0 || utf8Json.Length > JsonSupport.MaxJsonBytes)
        {
            throw new InvalidDataException("Manifest JSON size is invalid.");
        }

        var manifest = JsonSerializer.Deserialize<ReleaseManifest>(utf8Json, JsonSupport.CreateOptions())
            ?? throw new InvalidDataException("Manifest JSON is empty or invalid.");
        ManifestValidator.Validate(manifest, requireSignature: true);
        return manifest;
    }

    public static Task WriteAsync(
        string path,
        ReleaseManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ManifestValidator.Validate(manifest, requireSignature: true);
        return JsonSupport.WriteAtomicAsync(path, manifest, cancellationToken);
    }
}
