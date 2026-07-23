// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class UpdatePlanner
{
    public static async Task<UpdatePlan> CreateAsync(
        string installRoot,
        ReleaseManifest manifest,
        ManagedInstallState state,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ManifestValidator.Validate(manifest, requireSignature: true);
        InstallStateStore.Validate(state);

        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        var downloads = new List<ReleaseFile>();
        var totalFiles = manifest.Files.Count;
        var completedFiles = 0;

        foreach (var file in manifest.Files.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var localPath = SafePaths.ResolveManagedPath(root, file.Path);
            var matches = false;
            if (File.Exists(localPath))
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

        var managedPaths = state.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletes = new List<string>();
        var ignoredDeletes = new List<string>();
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

        return new UpdatePlan
        {
            Manifest = manifest,
            Downloads = downloads,
            Deletes = deletes,
            IgnoredDeletes = ignoredDeletes
        };
    }
}
