// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public sealed class TransactionalUpdater
{
    private sealed record BackupEntry(string DestinationPath, string BackupPath, bool HadOriginal);

    public async Task<UpdateResult> ApplyAsync(
        string installRoot,
        UpdatePlan plan,
        IContentSource contentSource,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(contentSource);
        ManifestValidator.Validate(plan.Manifest, requireSignature: true);

        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        SafePaths.EnsureNoReparsePoints(root, root);
        using var installLock = InstallLock.Acquire(root);
        _ = await TransactionRecovery.RecoverLockedAsync(root, progress, cancellationToken);

        var metadataRoot = InstallStateStore.GetMetadataRoot(root);
        Directory.CreateDirectory(metadataRoot);
        SafePaths.EnsureNoReparsePoints(root, metadataRoot);

        var transactionId = Guid.NewGuid().ToString("N");
        var transactionRoot = Path.Combine(metadataRoot, "transactions", transactionId);
        var stagingRoot = Path.Combine(transactionRoot, "staging");
        var rollbackRoot = Path.Combine(transactionRoot, "rollback");
        Directory.CreateDirectory(stagingRoot);
        Directory.CreateDirectory(rollbackRoot);
        var journal = await TransactionRecovery.BeginStagingAsync(
            transactionRoot,
            plan,
            cancellationToken);

        var downloadedBytes = 0L;
        var downloadedFiles = 0;
        var commitStarted = false;
        try
        {
            foreach (var file in plan.Downloads)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stagedPath = SafePaths.ResolveManagedPath(stagingRoot, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(stagedPath)!);
                await contentSource.DownloadVerifiedAsync(
                    file,
                    stagedPath,
                    progress,
                    downloadedBytes,
                    plan.DownloadBytes,
                    downloadedFiles,
                    plan.Downloads.Count,
                    cancellationToken);

                downloadedBytes = checked(downloadedBytes + file.Size);
                downloadedFiles++;
                progress?.Report(new UpdateProgress(
                    "download",
                    file.Path,
                    downloadedBytes,
                    plan.DownloadBytes,
                    downloadedFiles,
                    plan.Downloads.Count));
            }

            cancellationToken.ThrowIfCancellationRequested();
            journal = await TransactionRecovery.PrepareCommitAsync(
                root,
                transactionRoot,
                plan,
                journal,
                cancellationToken);
            progress?.Report(new UpdateProgress(
                "commit",
                null,
                downloadedBytes,
                plan.DownloadBytes,
                downloadedFiles,
                plan.Downloads.Count));

            commitStarted = true;
            return await CommitAsync(
                root,
                plan,
                stagingRoot,
                rollbackRoot,
                transactionRoot,
                journal,
                progress);
        }
        catch
        {
            if (!commitStarted)
            {
                TryDeleteDirectory(transactionRoot);
            }

            throw;
        }
    }

    private static async Task<UpdateResult> CommitAsync(
        string root,
        UpdatePlan plan,
        string stagingRoot,
        string rollbackRoot,
        string transactionRoot,
        TransactionJournal journal,
        IProgress<UpdateProgress>? progress)
    {
        var backups = new List<BackupEntry>();
        var installedDestinations = new List<string>();
        var statePath = InstallStateStore.GetStatePath(root);
        var stateBackupPath = Path.Combine(rollbackRoot, "__state", "state.json");
        var stateExisted = File.Exists(statePath);

        try
        {
            if (stateExisted)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(stateBackupPath)!);
                File.Copy(statePath, stateBackupPath, overwrite: false);
            }

            journal = await TransactionRecovery.MarkCommittingAsync(
                transactionRoot,
                journal,
                CancellationToken.None);

            foreach (var file in plan.Downloads)
            {
                var destination = SafePaths.ResolveManagedPath(root, file.Path);
                var staged = SafePaths.ResolveManagedPath(stagingRoot, file.Path);
                if (!File.Exists(staged))
                {
                    throw new InvalidDataException($"Staged file '{file.Path}' is missing before commit.");
                }

                var backup = SafePaths.ResolveManagedPath(rollbackRoot, file.Path);
                var hadOriginal = File.Exists(destination);
                if (hadOriginal)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Move(destination, backup, overwrite: false);
                }

                backups.Add(new BackupEntry(destination, backup, hadOriginal));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(staged, destination, overwrite: false);
                installedDestinations.Add(destination);
            }

            foreach (var deletePath in plan.Deletes)
            {
                var destination = SafePaths.ResolveManagedPath(root, deletePath);
                var backup = SafePaths.ResolveManagedPath(rollbackRoot, deletePath);
                var hadOriginal = File.Exists(destination);
                if (hadOriginal)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Move(destination, backup, overwrite: false);
                }

                backups.Add(new BackupEntry(destination, backup, hadOriginal));
            }

            var newState = InstallStateStore.FromManifest(plan.Manifest);
            await InstallStateStore.SaveAsync(root, newState, CancellationToken.None);
            journal = await TransactionRecovery.MarkStateSavedAsync(
                transactionRoot,
                journal,
                CancellationToken.None);

            progress?.Report(new UpdateProgress(
                "complete",
                null,
                plan.DownloadBytes,
                plan.DownloadBytes,
                plan.Downloads.Count,
                plan.Downloads.Count));

            TryDeleteDirectory(transactionRoot);
            return new UpdateResult(
                plan.Manifest.ReleaseId,
                plan.Downloads.Count,
                plan.Deletes.Count,
                plan.IgnoredDeletes);
        }
        catch (Exception commitException)
        {
            var rollbackErrors = new List<Exception>();
            foreach (var destination in installedDestinations.AsEnumerable().Reverse())
            {
                TryDeleteFile(destination, rollbackErrors);
            }

            foreach (var entry in backups.AsEnumerable().Reverse())
            {
                try
                {
                    if (entry.HadOriginal && File.Exists(entry.BackupPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(entry.DestinationPath)!);
                        if (File.Exists(entry.DestinationPath))
                        {
                            File.Delete(entry.DestinationPath);
                        }

                        File.Move(entry.BackupPath, entry.DestinationPath, overwrite: false);
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    rollbackErrors.Add(exception);
                }
            }

            try
            {
                if (stateExisted && File.Exists(stateBackupPath))
                {
                    File.Copy(stateBackupPath, statePath, overwrite: true);
                }
                else if (!stateExisted && File.Exists(statePath))
                {
                    File.Delete(statePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                rollbackErrors.Add(exception);
            }

            if (rollbackErrors.Count > 0)
            {
                throw new IOException(
                    $"Update commit failed and rollback was incomplete. Recovery data remains at '{transactionRoot}'.",
                    new AggregateException(new[] { commitException }.Concat(rollbackErrors)));
            }

            TryDeleteDirectory(transactionRoot);
            throw;
        }
    }

    private static void TryDeleteFile(string path, ICollection<Exception> errors)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(exception);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
