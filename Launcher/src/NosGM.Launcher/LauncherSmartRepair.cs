// SPDX-License-Identifier: MIT

using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal enum LauncherRepairStatus
{
    UpToDate,
    Repaired,
    Failed
}

internal sealed record LauncherRepairOutcome(
    LauncherRepairStatus Status,
    string ReleaseId,
    int DownloadedFiles,
    int DeletedFiles,
    long DownloadedBytes,
    int IgnoredDeletes);

internal sealed record LauncherRepairHistoryEntry
{
    public DateTimeOffset OccurredAtUtc { get; init; }
    public LauncherRepairStatus Status { get; init; }
    public string ReleaseId { get; init; } = string.Empty;
    public int DownloadedFiles { get; init; }
    public int DeletedFiles { get; init; }
    public long DownloadedBytes { get; init; }
    public int IgnoredDeletes { get; init; }
    public string FailureType { get; init; } = string.Empty;
}

internal sealed record LauncherRepairHistory
{
    public int SchemaVersion { get; init; } = 1;
    public IReadOnlyList<LauncherRepairHistoryEntry> Entries { get; init; }
        = Array.Empty<LauncherRepairHistoryEntry>();
}

internal sealed class LauncherSmartRepairService
{
    private const int MaximumHistoryEntries = 25;
    private readonly LauncherController _controller = new();

    private static string HistoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "repair-history.json");

    public bool IsAvailable => TrustedChannel.IsConfigured;

    public async Task<LauncherRepairOutcome> RepairAsync(
        LauncherSettings settings,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "This launcher build has no trusted release channel and cannot repair files.");
        }

        try
        {
            var operation = await _controller.CheckAndApplyAsync(
                    settings,
                    apply: true,
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);

            var result = operation.Result;
            var status = result is null
                ? LauncherRepairStatus.UpToDate
                : LauncherRepairStatus.Repaired;
            var outcome = new LauncherRepairOutcome(
                status,
                result?.ReleaseId ?? operation.Plan.Manifest.ReleaseId,
                result?.DownloadedFiles ?? 0,
                result?.DeletedFiles ?? 0,
                operation.Plan.DownloadBytes,
                result?.IgnoredDeletes.Count ?? operation.Plan.IgnoredDeletes.Count);

            await AppendHistoryAsync(
                    new LauncherRepairHistoryEntry
                    {
                        OccurredAtUtc = DateTimeOffset.UtcNow,
                        Status = outcome.Status,
                        ReleaseId = Limit(outcome.ReleaseId, 128),
                        DownloadedFiles = outcome.DownloadedFiles,
                        DeletedFiles = outcome.DeletedFiles,
                        DownloadedBytes = outcome.DownloadedBytes,
                        IgnoredDeletes = outcome.IgnoredDeletes
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
            return outcome;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await AppendHistoryAsync(
                    new LauncherRepairHistoryEntry
                    {
                        OccurredAtUtc = DateTimeOffset.UtcNow,
                        Status = LauncherRepairStatus.Failed,
                        FailureType = Limit(exception.GetType().Name, 80)
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    public static async Task<LauncherRepairHistory> ReadHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(HistoryPath))
        {
            return new LauncherRepairHistory();
        }

        try
        {
            var history = await JsonSupport.ReadAsync<LauncherRepairHistory>(
                    HistoryPath,
                    cancellationToken)
                .ConfigureAwait(false);
            if (history.SchemaVersion != 1 || history.Entries.Count > MaximumHistoryEntries)
            {
                throw new InvalidDataException("Launcher repair history is invalid.");
            }

            return history with
            {
                Entries = history.Entries
                    .Take(MaximumHistoryEntries)
                    .ToArray()
            };
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new LauncherRepairHistory();
        }
    }

    private static async Task AppendHistoryAsync(
        LauncherRepairHistoryEntry entry,
        CancellationToken cancellationToken)
    {
        LauncherRepairHistory current;
        try
        {
            current = await ReadHistoryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var entries = new[] { entry }
            .Concat(current.Entries)
            .Take(MaximumHistoryEntries)
            .ToArray();
        try
        {
            await JsonSupport.WriteAtomicAsync(
                    HistoryPath,
                    new LauncherRepairHistory { Entries = entries },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // Repair must not fail only because optional local history could not be written.
        }
    }

    private static string Limit(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
