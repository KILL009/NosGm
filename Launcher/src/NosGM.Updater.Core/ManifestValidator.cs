// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class ManifestValidator
{
    private const int MaxFiles = 50_000;
    private const long MaxSingleFileBytes = 16L * 1024 * 1024 * 1024;
    private const long MaxReleaseBytes = 128L * 1024 * 1024 * 1024;

    public static void Validate(ReleaseManifest manifest, bool requireSignature)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported manifest schema {manifest.SchemaVersion}.");
        }

        RequireText(manifest.ReleaseId, nameof(manifest.ReleaseId), 128);
        RequireText(manifest.ClientVersion, nameof(manifest.ClientVersion), 64);
        RequireText(manifest.MinimumLauncherVersion, nameof(manifest.MinimumLauncherVersion), 32);
        RequireText(manifest.KeyId, nameof(manifest.KeyId), 64);

        if (!string.Equals(
                manifest.SignatureAlgorithm,
                ReleaseAlgorithms.EcdsaP256Sha256,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported signature algorithm '{manifest.SignatureAlgorithm}'.");
        }

        if (requireSignature && string.IsNullOrWhiteSpace(manifest.Signature))
        {
            throw new InvalidDataException("Manifest signature is required.");
        }

        if (manifest.Signature is null ||
            manifest.Signature.Length > 512 ||
            manifest.Signature.Any(char.IsWhiteSpace))
        {
            throw new InvalidDataException("Manifest signature encoding is invalid.");
        }

        if (manifest.Files is null || manifest.Delete is null)
        {
            throw new InvalidDataException("Manifest file and delete collections cannot be null.");
        }

        if (manifest.Files.Count > MaxFiles || manifest.Delete.Count > MaxFiles)
        {
            throw new InvalidDataException($"A manifest cannot contain more than {MaxFiles} file operations.");
        }

        var filePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        foreach (var file in manifest.Files)
        {
            if (file is null)
            {
                throw new InvalidDataException("Manifest contains a null file entry.");
            }

            var path = SafePaths.NormalizeRelativePath(file.Path);
            if (!string.Equals(path, file.Path, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Manifest path '{file.Path}' is not canonical.");
            }

            if (!filePaths.Add(path))
            {
                throw new InvalidDataException($"Manifest contains duplicate file path '{path}'.");
            }

            if (file.Size < 0 || file.Size > MaxSingleFileBytes)
            {
                throw new InvalidDataException($"Manifest file '{path}' has an invalid size.");
            }

            totalBytes = checked(totalBytes + file.Size);
            if (totalBytes > MaxReleaseBytes)
            {
                throw new InvalidDataException("Manifest exceeds the maximum supported release size.");
            }

            if (!Hashing.IsSha256(file.Sha256))
            {
                throw new InvalidDataException($"Manifest file '{path}' has an invalid SHA-256.");
            }

            var normalizedUrl = SafePaths.NormalizeRelativePath(file.Url);
            if (!string.Equals(normalizedUrl, file.Url, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Manifest URL '{file.Url}' is not canonical.");
            }
        }

        var deletePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var deletePath in manifest.Delete)
        {
            var normalized = SafePaths.NormalizeRelativePath(deletePath);
            if (!string.Equals(normalized, deletePath, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Delete path '{deletePath}' is not canonical.");
            }

            if (!deletePaths.Add(normalized))
            {
                throw new InvalidDataException($"Manifest contains duplicate delete path '{normalized}'.");
            }

            if (filePaths.Contains(normalized))
            {
                throw new InvalidDataException(
                    $"Path '{normalized}' cannot be downloaded and deleted in the same manifest.");
            }
        }
    }

    private static void RequireText(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > maxLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            value.Any(char.IsControl))
        {
            throw new InvalidDataException($"Manifest field '{name}' is invalid.");
        }
    }
}
