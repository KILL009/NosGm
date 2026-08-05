// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal static class TrustedChannel
{
    private sealed record ResolvedChannel(
        string ManifestUriText,
        string ContentBaseUriText,
        string KeyId,
        string PublicKeyPem,
        bool IsLocalDevelopment);

    public static string ManifestUriText
        => Resolve()?.ManifestUriText ?? TrustedChannelConfiguration.ManifestUriText;

    public static string ContentBaseUriText
        => Resolve()?.ContentBaseUriText ?? TrustedChannelConfiguration.ContentBaseUriText;

    public static string KeyId
        => Resolve()?.KeyId ?? TrustedChannelConfiguration.KeyId;

    public static string PublicKeyPem
        => Resolve()?.PublicKeyPem ?? TrustedChannelConfiguration.PublicKeyPem;

    public static bool IsConfigured => Resolve() is not null;

    public static bool IsLocalDevelopmentChannel
        => Resolve()?.IsLocalDevelopment == true;

    internal static bool UsesPlaceholderConfiguration
        => string.Equals(
               TrustedChannelConfiguration.KeyId,
               "UNCONFIGURED",
               StringComparison.Ordinal) &&
           string.IsNullOrWhiteSpace(TrustedChannelConfiguration.PublicKeyPem) &&
           Uri.TryCreate(
               TrustedChannelConfiguration.ManifestUriText,
               UriKind.Absolute,
               out var manifestUri) &&
           Uri.TryCreate(
               TrustedChannelConfiguration.ContentBaseUriText,
               UriKind.Absolute,
               out var contentUri) &&
           manifestUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) &&
           contentUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);

    public static Uri ManifestUri
        => Resolve() is { } channel
            ? new Uri(channel.ManifestUriText, UriKind.Absolute)
            : throw new InvalidOperationException("The trusted release channel is not configured.");

    public static Uri ContentBaseUri
        => Resolve() is { } channel
            ? new Uri(channel.ContentBaseUriText, UriKind.Absolute)
            : throw new InvalidOperationException("The trusted release channel is not configured.");

    private static ResolvedChannel? Resolve()
    {
        if (TryValidateCompiledChannel(out var compiled))
        {
            return compiled;
        }

        // The local configuration reader performs the same URI validation with
        // allowLoopbackHttp: true, but only after this exact placeholder gate.
        if (!UsesPlaceholderConfiguration ||
            !LocalDevelopmentRepairChannel.TryReadConfiguration(out var local))
        {
            return null;
        }

        return new ResolvedChannel(
            local.ManifestUriText,
            local.ContentBaseUriText,
            local.KeyId,
            local.PublicKeyPem,
            IsLocalDevelopment: true);
    }

    private static bool TryValidateCompiledChannel(out ResolvedChannel channel)
    {
        channel = null!;
        if (!TryValidateCleanChannelUris(
                TrustedChannelConfiguration.ManifestUriText,
                TrustedChannelConfiguration.ContentBaseUriText,
                allowLoopbackHttp: false,
                out _,
                out _) ||
            string.Equals(
                TrustedChannelConfiguration.KeyId,
                "UNCONFIGURED",
                StringComparison.Ordinal) ||
            !TrustedChannelConfiguration.PublicKeyPem.Contains(
                "BEGIN PUBLIC KEY",
                StringComparison.Ordinal))
        {
            return false;
        }

        channel = new ResolvedChannel(
            TrustedChannelConfiguration.ManifestUriText,
            TrustedChannelConfiguration.ContentBaseUriText,
            TrustedChannelConfiguration.KeyId,
            TrustedChannelConfiguration.PublicKeyPem,
            IsLocalDevelopment: false);
        return true;
    }

    internal static bool TryValidateCleanChannelUris(
        string manifestUriText,
        string contentBaseUriText,
        bool allowLoopbackHttp,
        out Uri manifestUri,
        out Uri contentUri)
    {
        manifestUri = null!;
        contentUri = null!;
        if (!Uri.TryCreate(manifestUriText, UriKind.Absolute, out var parsedManifestUri) ||
            parsedManifestUri is null ||
            !Uri.TryCreate(contentBaseUriText, UriKind.Absolute, out var parsedContentUri) ||
            parsedContentUri is null)
        {
            return false;
        }

        manifestUri = parsedManifestUri;
        contentUri = parsedContentUri;

        var manifestTransportAllowed =
            string.Equals(
                manifestUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase) ||
            allowLoopbackHttp &&
            manifestUri.IsLoopback &&
            string.Equals(
                manifestUri.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase);
        var contentTransportAllowed =
            string.Equals(
                contentUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase) ||
            allowLoopbackHttp &&
            contentUri.IsLoopback &&
            string.Equals(
                contentUri.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase);

        return manifestTransportAllowed &&
               contentTransportAllowed &&
               string.IsNullOrEmpty(manifestUri.UserInfo) &&
               string.IsNullOrEmpty(manifestUri.Query) &&
               string.IsNullOrEmpty(manifestUri.Fragment) &&
               string.IsNullOrEmpty(contentUri.UserInfo) &&
               string.IsNullOrEmpty(contentUri.Query) &&
               string.IsNullOrEmpty(contentUri.Fragment) &&
               contentUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal) &&
               !manifestUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) &&
               !contentUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);
    }
}
