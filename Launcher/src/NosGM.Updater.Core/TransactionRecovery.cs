// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public sealed record RecoveryResult(
    int RecoveredTransactions,
    int FinalizedTransactions,
    int DiscardedTransactions);

internal static class TransactionJournalPhases
{
    public const string Staging = "staging";
    public const string Prepared = "prepared";
    public const string Committing = "committing";
    public const string StateSaved = "state-saved";
}

internal sealed record TransactionJournalOperation
{
    public string Path { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public bool HadOriginal { get; init; }
}

internal sealed record TransactionJournal
{
    public int SchemaVersion { get; init; } = 1;
    public string TransactionId { get; init; } = string.Empty;
    public string Phase { get; init; } = TransactionJournalPhases.Staging;
    public bool StateExisted { get; init; }
    public ManagedInstallState TargetState { get; init; } = new();
    public IReadOnlyList<TransactionJournalOperation> Operations { get; init; }
        = Array.Empty<TransactionJournalOperation>();
}

public static class TransactionRecovery
{
    private const string JournalFileName = "journal.json";

    public static async Task<RecoveryResult> RecoverAsync(
        string installRoot,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        using var installLock = InstallLock.Acquire(root);
        return await RecoverLockedAsync(root, progress, cancellationToken);
    }

    internal static async Task<RecoveryResult> RecoverLockedAsync(
        string installRoot,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(installRoot);
        var transactionsRoot = GetTransactionsRoot(root);
        if (!Directory.Exists(transactionsRoot))
        {
            return new RecoveryResult(0, 0, 0);
        }

        SafePaths.EnsureNoReparsePoints(root, transactionsRoot);
        var recovered = 0;
        var finalized = 0;
        var discarded = 0;

        foreach (var transactionRoot in Directory.EnumerateDirectories(transactionsRoot)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SafePaths.EnsureNoReparsePoints(root, transactionRoot);
            var journalPath = Path.Combine(transactionRoot, JournalFileName);
            if (!File.Exists(journalPath))
            {
                if (HasRollbackEvidence(transactionRoot))
                {
                    throw new InvalidDataException(
                        $"Transaction '{Path.GetFileName(transactionRoot)}' has rollback data but no journal.");
                }

                Directory.Delete(transactionRoot, recursive: true);
                discarded++;
                continue;
            }

            var journal = await JsonSupport.ReadAsync<TransactionJournal>(journalPath, cancellationToken);
            Validate(journal, Path.GetFileName(transactionRoot));
            progress?.Report(new UpdateProgress(
                "recovery",
                journal.TransactionId,
                recovered + finalized + discarded,
                0,
                recovered + finalized + discarded,
                0));

            var currentState = await InstallStateStore.LoadAsync(root, cancellationToken);
            if (StateEquivalent(currentState, journal.TargetState))
            {
                Directory.Delete(transactionRoot, recursive: true);
                finalized++;
                continue;
            }

            if (journal.Phase is TransactionJournalPhases.Staging or TransactionJournalPhases.Prepared)
            {
                Directory.Delete(transactionRoot, recursive: true);
                discarded++;
                continue;
            }

            await RollBackAsync(root, transactionRoot, journal, cancellationToken);
            Directory.Delete(transactionRoot, recursive: true);
            recovered++;
        }

        return new RecoveryResult(recovered, finalized, discarded);
    }

    internal static async Task<TransactionJournal> BeginStagingAsync(
        string transactionRoot,
        UpdatePlan plan,
        CancellationToken cancellationToken)
    {
        var journal = new TransactionJournal
        {
            TransactionId = Path.GetFileName(transactionRoot),
            Phase = TransactionJournalPhases.Staging,
            TargetState = InstallStateStore.FromManifest(plan.Manifest),
            Operations = Array.Empty<TransactionJournalOperation>()
        };
        await WriteAsync(transactionRoot, journal, cancellationToken);
        return journal;
    }

    internal static async Task<TransactionJournal> PrepareCommitAsync(
        string installRoot,
        string transactionRoot,
        UpdatePlan plan,
        TransactionJournal journal,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(installRoot);
        var operations = new List<TransactionJournalOperation>();
        foreach (var file in plan.Downloads)
        {
            var destination = SafePaths.ResolveManagedPath(root, file.Path);
            if (Directory.Exists(destination))
            {
                throw new InvalidDataException($"Managed file '{file.Path}' conflicts with a directory before commit.");
            }

            operations.Add(new TransactionJournalOperation
            {
                Path = file.Path,
                Kind = "replace",
                HadOriginal = File.Exists(destination)
            });
        }

        foreach (var path in plan.Deletes)
        {
            var destination = SafePaths.ResolveManagedPath(root, path);
            if (Directory.Exists(destination))
            {
                throw new InvalidDataException($"Managed deletion '{path}' conflicts with a directory before commit.");
            }

            operations.Add(new TransactionJournalOperation
            {
                Path = path,
                Kind = "delete",
                HadOriginal = File.Exists(destination)
            });
        }

        var prepared = journal with
        {
            Phase = TransactionJournalPhases.Prepared,
            StateExisted = File.Exists(InstallStateStore.GetStatePath(root)),
            Operations = Array.AsReadOnly(operations.ToArray())
        };
        Validate(prepared, prepared.TransactionId);
        await WriteAsync(transactionRoot, prepared, cancellationToken);
        return prepared;
    }

    internal static async Task<TransactionJournal> MarkCommittingAsync(
        string transactionRoot,
        TransactionJournal journal,
        CancellationToken cancellationToken)
    {
        var committing = journal with { Phase = TransactionJournalPhases.Committing };
        await WriteAsync(transactionRoot, committing, cancellationToken);
        return committing;
    }

    internal static async Task<TransactionJournal> MarkStateSavedAsync(
        string transactionRoot,
        TransactionJournal journal,
        CancellationToken cancellationToken)
    {
        var saved = journal with { Phase = TransactionJournalPhases.StateSaved };
        await WriteAsync(transactionRoot, saved, cancellationToken);
        return saved;
    }

    private static string GetTransactionsRoot(string installRoot)
        => Path.Combine(InstallStateStore.GetMetadataRoot(installRoot), "transactions");

    private static Task WriteAsync(
        string transactionRoot,
        TransactionJournal journal,
        CancellationToken cancellationToken)
        => JsonSupport.WriteAtomicAsync(
            Path.Combine(transactionRoot, JournalFileName),
            journal,
            cancellationToken);

    private static async Task RollBackAsync(
        string root,
        string transactionRoot,
        TransactionJournal journal,
        CancellationToken cancellationToken)
    {
        var rollbackRoot = Path.Combine(transactionRoot, "rollback");
        foreach (var operation in journal.Operations.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = SafePaths.ResolveManagedPath(root, operation.Path);
            var backup = SafePaths.ResolveManagedPath(rollbackRoot, operation.Path);
            if (Directory.Exists(destination) || Directory.Exists(backup))
            {
                throw new InvalidDataException(
                    $"Recovery path '{operation.Path}' unexpectedly resolves to a directory.");
            }

            if (operation.HadOriginal)
            {
                if (File.Exists(backup))
                {
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Move(backup, destination, overwrite: false);
                }
                else if (!File.Exists(destination))
                {
                    throw new IOException(
                        $"Recovery cannot restore '{operation.Path}' because both destination and backup are missing.");
                }
            }
            else if (string.Equals(operation.Kind, "replace", StringComparison.Ordinal) && File.Exists(destination))
            {
                File.Delete(destination);
            }
        }

        var statePath = InstallStateStore.GetStatePath(root);
        var stateBackupPath = Path.Combine(rollbackRoot, "__state", "state.json");
        if (journal.StateExisted)
        {
            if (File.Exists(stateBackupPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
                File.Copy(stateBackupPath, statePath, overwrite: true);
            }
            else if (!File.Exists(statePath))
            {
                throw new IOException("Recovery cannot restore the previous managed state.");
            }
        }
        else if (File.Exists(statePath))
        {
            File.Delete(statePath);
        }

        _ = await InstallStateStore.LoadAsync(root, cancellationToken);
    }

    private static bool HasRollbackEvidence(string transactionRoot)
    {
        var rollbackRoot = Path.Combine(transactionRoot, "rollback");
        return Directory.Exists(rollbackRoot) && Directory.EnumerateFileSystemEntries(rollbackRoot).Any();
    }

    private static bool StateEquivalent(ManagedInstallState left, ManagedInstallState right)
    {
        if (!string.Equals(left.ReleaseId, right.ReleaseId, StringComparison.Ordinal) ||
            !string.Equals(left.ClientVersion, right.ClientVersion, StringComparison.Ordinal) ||
            left.Files.Count != right.Files.Count)
        {
            return false;
        }

        foreach (var pair in right.Files)
        {
            if (!left.Files.TryGetValue(pair.Key, out var current) ||
                current.Size != pair.Value.Size ||
                !string.Equals(current.Sha256, pair.Value.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static void Validate(TransactionJournal journal, string expectedTransactionId)
    {
        if (journal.SchemaVersion != 1 ||
            !string.Equals(journal.TransactionId, expectedTransactionId, StringComparison.Ordinal) ||
            journal.TransactionId.Length is < 1 or > 64 ||
            journal.Operations is null ||
            journal.TargetState is null)
        {
            throw new InvalidDataException("Transaction journal is unsupported or incomplete.");
        }

        if (journal.Phase is not (
                TransactionJournalPhases.Staging or
                TransactionJournalPhases.Prepared or
                TransactionJournalPhases.Committing or
                TransactionJournalPhases.StateSaved))
        {
            throw new InvalidDataException($"Transaction phase '{journal.Phase}' is unsupported.");
        }

        InstallStateStore.Validate(journal.TargetState);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var operation in journal.Operations)
        {
            var normalized = SafePaths.NormalizeRelativePath(operation.Path);
            if (!string.Equals(normalized, operation.Path, StringComparison.Ordinal) ||
                !paths.Add(normalized) ||
                operation.Kind is not ("replace" or "delete"))
            {
                throw new InvalidDataException("Transaction journal contains an invalid operation.");
            }
        }
    }
}
