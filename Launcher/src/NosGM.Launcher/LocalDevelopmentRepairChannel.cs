// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed record LocalTrustedChannelConfiguration
{
    public int SchemaVersion { get; init; } = 1;
    public string ManifestUriText { get; init; } = string.Empty;
    public string ContentBaseUriText { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
    public string PublicKeyPem { get; init; } = string.Empty;
}

/// <summary>
/// Creates a loopback-only signed repair channel for source-built development
/// launchers. Published launchers ignore this file because their compiled
/// channel is already configured with the official public key.
/// </summary>
internal static class LocalDevelopmentRepairChannel
{
    private const long MaximumConfigurationBytes = 64 * 1024;
    private const string ManifestRoute = "/local-update/release-manifest.json";
    private const string ContentRoute = "/local-update/content/";

    private static readonly SemaphoreSlim BootstrapGate = new(1, 1);
    private static readonly JsonSerializerOptions StrictJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    internal static string RootPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "local-repair-channel");

    internal static string ContentRootPath => Path.Combine(RootPath, "content");
    internal static string ManifestPath => Path.Combine(RootPath, "release-manifest.json");
    internal static string ConfigurationPath => Path.Combine(RootPath, "trusted-channel.json");

    public static async Task<bool> EnsureAsync(
        LauncherSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!TrustedChannel.UsesPlaceholderConfiguration)
        {
            return TrustedChannel.IsConfigured;
        }

        if (TryReadConfiguration(out _))
        {
            return true;
        }

        await BootstrapGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryReadConfiguration(out _))
            {
                return true;
            }

            if (!TryResolveLoopbackPortal(settings.PortalBaseUri, out var portalBase))
            {
                return false;
            }

            var gamePath = SafePaths.ResolveManagedPath(
                settings.InstallRoot,
                settings.GameExecutable);
            if (!File.Exists(gamePath))
            {
                return false;
            }

            Directory.CreateDirectory(ContentRootPath);
            SafePaths.EnsureNoReparsePoints(RootPath, ContentRootPath);
            var contentPath = SafePaths.ResolveManagedPath(
                ContentRootPath,
                settings.GameExecutable);
            await CopyAtomicAsync(gamePath, contentPath, cancellationToken)
                .ConfigureAwait(false);

            var contentInfo = new FileInfo(contentPath);
            var sha256 = await Hashing.Sha256FileAsync(contentPath, cancellationToken)
                .ConfigureAwait(false);
            var fileVersion = FileVersionInfo.GetVersionInfo(gamePath).FileVersion;
            var normalizedVersion = string.IsNullOrWhiteSpace(fileVersion)
                ? "local"
                : fileVersion.Trim();
            var shortHash = sha256[..12].ToLowerInvariant();
            var keyId = $"nosgm-local-{shortHash}";
            var releaseId = $"local-{LimitIdentifier(normalizedVersion, 32)}-{shortHash}";

            using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKeyPem = signingKey.ExportECPrivateKeyPem();
            var publicKeyPem = signingKey.ExportSubjectPublicKeyInfoPem();

            var unsignedManifest = new ReleaseManifest
            {
                ReleaseId = releaseId,
                ClientVersion = normalizedVersion,
                MinimumLauncherVersion = "0.0.0",
                KeyId = keyId,
                Files =
                [
                    new ReleaseFile
                    {
                        Path = settings.GameExecutable,
                        Url = settings.GameExecutable,
                        Size = contentInfo.Length,
                        Sha256 = sha256
                    }
                ],
                Delete = Array.Empty<string>(),
                Signature = string.Empty
            };
            var signedManifest = unsignedManifest with
            {
                Signature = ManifestSecurity.Sign(unsignedManifest, privateKeyPem)
            };
            await ManifestIO.WriteAsync(
                    ManifestPath,
                    signedManifest,
                    cancellationToken)
                .ConfigureAwait(false);

            var manifestUri = new Uri(portalBase, ManifestRoute);
            var contentUri = new Uri(portalBase, ContentRoute);
            var configuration = new LocalTrustedChannelConfiguration
            {
                ManifestUriText = manifestUri.AbsoluteUri,
                ContentBaseUriText = contentUri.AbsoluteUri,
                KeyId = keyId,
                PublicKeyPem = publicKeyPem
            };
            await JsonSupport.WriteAtomicAsync(
                    ConfigurationPath,
                    configuration,
                    cancellationToken)
                .ConfigureAwait(false);

            return TryReadConfiguration(out _);
        }
        finally
        {
            BootstrapGate.Release();
        }
    }

    internal static bool TryReadConfiguration(
        out LocalTrustedChannelConfiguration configuration)
    {
        configuration = null!;
        try
        {
            var info = new FileInfo(ConfigurationPath);
            if (!info.Exists ||
                info.Length <= 0 ||
                info.Length > MaximumConfigurationBytes ||
                !File.Exists(ManifestPath) ||
                !Directory.Exists(ContentRootPath))
            {
                return false;
            }

            var json = File.ReadAllText(ConfigurationPath);
            var candidate = JsonSerializer.Deserialize<LocalTrustedChannelConfiguration>(
                json,
                StrictJsonOptions);
            if (candidate is null ||
                candidate.SchemaVersion != 1 ||
                !IsSafeKeyId(candidate.KeyId) ||
                !TryValidatePublicKey(candidate.PublicKeyPem) ||
                !TrustedChannel.TryValidateCleanChannelUris(
                    candidate.ManifestUriText,
                    candidate.ContentBaseUriText,
                    allowLoopbackHttp: true,
                    out var manifestUri,
                    out var contentUri) ||
                !manifestUri.IsLoopback ||
                !contentUri.IsLoopback ||
                !string.Equals(
                    manifestUri.GetLeftPart(UriPartial.Authority),
                    contentUri.GetLeftPart(UriPartial.Authority),
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    manifestUri.AbsolutePath,
                    ManifestRoute,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    contentUri.AbsolutePath,
                    ContentRoute,
                    StringComparison.Ordinal))
            {
                return false;
            }

            configuration = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or
                CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    private static bool TryResolveLoopbackPortal(string value, out Uri portalBase)
    {
        portalBase = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            !candidate.IsLoopback ||
            !string.Equals(
                candidate.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment))
        {
            return false;
        }

        var builder = new UriBuilder(candidate)
        {
            Path = "/",
            Query = string.Empty,
            Fragment = string.Empty
        };
        portalBase = builder.Uri;
        return true;
    }

    private static async Task CopyAtomicAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var temporaryPath = destinationPath + $".tmp.{Guid.NewGuid():N}";
        try
        {
            await using (var source = new FileStream(
                             sourcePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.ReadWrite,
                             128 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destination = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             128 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(destination, 128 * 1024, cancellationToken)
                    .ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                destination.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool TryValidatePublicKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            return false;
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(value);
        return key.KeySize == 256;
    }

    private static bool IsSafeKeyId(string value)
        => value.StartsWith("nosgm-local-", StringComparison.Ordinal) &&
           value.Length <= 64 &&
           value.All(character =>
               char.IsAsciiLetterOrDigit(character) ||
               character is '.' or '_' or '-');

    private static string LimitIdentifier(string value, int maximumLength)
    {
        var filtered = new string(value
            .Select(character =>
                char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_'
                    ? character
                    : '-')
            .ToArray());
        if (filtered.Length == 0)
        {
            filtered = "local";
        }

        return filtered.Length <= maximumLength
            ? filtered
            : filtered[..maximumLength];
    }
}
