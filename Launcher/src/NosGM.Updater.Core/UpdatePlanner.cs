// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class UpdatePlanner
{
    public static async Task<UpdatePlan> CreateAsync(
        string installRoot,
        VerifiedReleaseManifest verifiedManifest,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedManifest);
        var manifest = verifiedManifest.Manifest;
        ManifestValidator.Validate(manifest, requireSignature: true);

        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        var state = await InstallStateStore.LoadAsync(root, cancellationToken);
        var downloads = new List<ReleaseFile>();
        var totalFiles = manifest.Files.Count;
        var completedFiles = 0;
        var managedPaths = state.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in manifest.Files.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var localPath = SafePaths.ResolveManagedPath(root, file.Path);
            if (Directory.Exists(localPath))
            {
                throw new InvalidDataException(
                    $"Release file '{file.Path}' conflicts with an existing directory.");
            }

            var exists = File.Exists(localPath);
            var matches = false;
            if (exists)
            {
                var info = new FileInfo(localPath);
                if (info.Length == file.Size)
                {
                    var actualHash = await Hashing.Sha256FileAsync(localPath, cancellationToken);
                    matches = string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!matches)
            {
                if (exists && !managedPaths.Contains(file.Path))
                {
                    throw new InvalidDataException(
                        $"Release file '{file.Path}' conflicts with an existing file not managed by NosGM.");
                }

                downloads.Add(file);
            }

            completedFiles++;
            progress?.Report(new UpdateProgress(
                "scan",
                file.Path,
                0,
                0,
                completedFiles,
                totalFiles));
        }

        var desiredPaths = manifest.Files
            .Select(file => file.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletes = state.Files.Keys
            .Where(path => !desiredPaths.Contains(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ignoredDeletes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var deletePath in manifest.Delete.OrderBy(item => item, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = SafePaths.ResolveManagedPath(root, deletePath);
            if (managedPaths.Contains(deletePath))
            {
                deletes.Add(deletePath);
            }
            else
            {
                ignoredDeletes.Add(deletePath);
            }
        }

        return new UpdatePlan(
            manifest,
            downloads,
            deletes.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            ignoredDeletes.OrderBy(path => path, StringComparer.Ordinal).ToArray());
    }
}
