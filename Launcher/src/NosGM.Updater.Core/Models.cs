// SPDX-License-Identifier: MIT

namespace NosGM.Updater.Core;

public static class ReleaseAlgorithms
{
    public const string EcdsaP256Sha256 = "ECDSA_P256_SHA256";
}

public sealed record ReleaseManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string ReleaseId { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
    public string MinimumLauncherVersion { get; init; } = string.Empty;
    public string SignatureAlgorithm { get; init; } = ReleaseAlgorithms.EcdsaP256Sha256;
    public string KeyId { get; init; } = string.Empty;
    public IReadOnlyList<ReleaseFile> Files { get; init; } = Array.Empty<ReleaseFile>();
    public IReadOnlyList<string> Delete { get; init; } = Array.Empty<string>();
    public string Signature { get; init; } = string.Empty;
}

public sealed record ReleaseFile
{
    public string Path { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public sealed record ManagedFileState
{
    public long Size { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}

public sealed record ManagedInstallState
{
    public int SchemaVersion { get; init; } = 1;
    public string ReleaseId { get; init; } = string.Empty;
    public string ClientVersion { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, ManagedFileState> Files { get; init; }
        = new Dictionary<string, ManagedFileState>(StringComparer.OrdinalIgnoreCase);
}

public sealed class VerifiedReleaseManifest
{
    internal VerifiedReleaseManifest(ReleaseManifest manifest, string publicKeyFingerprint)
    {
        var files = manifest.Files.Select(file => file with { }).ToArray();
        var deletes = manifest.Delete.ToArray();
        Manifest = manifest with
        {
            Files = Array.AsReadOnly(files),
            Delete = Array.AsReadOnly(deletes)
        };
        PublicKeyFingerprint = publicKeyFingerprint;
    }

    public ReleaseManifest Manifest { get; }
    public string PublicKeyFingerprint { get; }
}

public sealed record UpdatePlan
{
    internal UpdatePlan(
        ReleaseManifest manifest,
        IReadOnlyList<ReleaseFile> downloads,
        IReadOnlyList<string> deletes,
        IReadOnlyList<string> ignoredDeletes)
    {
        Manifest = manifest;
        Downloads = Array.AsReadOnly(downloads.ToArray());
        Deletes = Array.AsReadOnly(deletes.ToArray());
        IgnoredDeletes = Array.AsReadOnly(ignoredDeletes.ToArray());
    }

    public ReleaseManifest Manifest { get; }
    public IReadOnlyList<ReleaseFile> Downloads { get; }
    public IReadOnlyList<string> Deletes { get; }
    public IReadOnlyList<string> IgnoredDeletes { get; }
    public long DownloadBytes => Downloads.Sum(file => file.Size);
}

public sealed record UpdateProgress(
    string Phase,
    string? Path,
    long CompletedBytes,
    long TotalBytes,
    int CompletedFiles,
    int TotalFiles);

public sealed record UpdateResult(
    string ReleaseId,
    int DownloadedFiles,
    int DeletedFiles,
    IReadOnlyList<string> IgnoredDeletes);

public interface IContentSource : IAsyncDisposable
{
    Task DownloadVerifiedAsync(
        ReleaseFile file,
        string destinationPath,
        IProgress<UpdateProgress>? progress,
        long alreadyCompletedBytes,
        long totalBytes,
        int completedFiles,
        int totalFiles,
        CancellationToken cancellationToken);
}
