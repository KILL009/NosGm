// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public sealed record ImportResult(
    string TargetReleaseId,
    int ManagedFiles,
    int MatchingFiles,
    int RepairFiles,
    int MissingFiles);

public static class ExistingInstallImporter
{
    public static async Task<ImportResult> AdoptAsync(
        string installRoot,
        VerifiedReleaseManifest verifiedManifest,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedManifest);
        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        SafePaths.EnsureNoReparsePoints(root, root);

        using var installLock = InstallLock.Acquire(root);
        _ = await TransactionRecovery.RecoverLockedAsync(root, progress, cancellationToken);
        var previousState = await InstallStateStore.LoadAsync(root, cancellationToken);
        if (previousState.Files.Count > 0 || !string.IsNullOrEmpty(previousState.ReleaseId))
        {
            throw new InvalidOperationException("This installation is already managed by NosGM.");
        }

        var manifest = verifiedManifest.Manifest;
        var managed = new Dictionary<string, ManagedFileState>(StringComparer.OrdinalIgnoreCase);
        var matching = 0;
        var repair = 0;
        var missing = 0;
        var completed = 0;

        foreach (var file in manifest.Files.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var localPath = SafePaths.ResolveManagedPath(root, file.Path);
            if (Directory.Exists(localPath))
            {
                throw new InvalidDataException(
                    $"Signed release path '{file.Path}' conflicts with an existing directory.");
            }

            if (!File.Exists(localPath))
            {
                missing++;
            }
            else
            {
                var size = new FileInfo(localPath).Length;
                var sha256 = await Hashing.Sha256FileAsync(localPath, cancellationToken);
                managed.Add(file.Path, new ManagedFileState
                {
                    Size = size,
                    Sha256 = sha256
                });

                if (size == file.Size &&
                    string.Equals(sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    matching++;
                }
                else
                {
                    repair++;
                }
            }

            completed++;
            progress?.Report(new UpdateProgress(
                "import",
                file.Path,
                0,
                0,
                completed,
                manifest.Files.Count));
        }

        if (managed.Count == 0)
        {
            throw new InvalidOperationException(
                "No existing files matched paths from the signed release manifest; nothing was imported.");
        }

        var importedState = new ManagedInstallState
        {
            ReleaseId = $"import-pending:{manifest.ReleaseId}",
            ClientVersion = manifest.ClientVersion,
            Files = managed
        };
        await InstallStateStore.SaveAsync(root, importedState, cancellationToken);
        return new ImportResult(manifest.ReleaseId, managed.Count, matching, repair, missing);
    }
}
