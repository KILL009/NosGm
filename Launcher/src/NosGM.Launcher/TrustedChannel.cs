// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal static class TrustedChannel
{
    public static string ManifestUriText => TrustedChannelConfiguration.ManifestUriText;
    public static string ContentBaseUriText => TrustedChannelConfiguration.ContentBaseUriText;
    public static string KeyId => TrustedChannelConfiguration.KeyId;
    public static string PublicKeyPem => TrustedChannelConfiguration.PublicKeyPem;

    public static bool IsConfigured
        => Uri.TryCreate(ManifestUriText, UriKind.Absolute, out var manifestUri) &&
           Uri.TryCreate(ContentBaseUriText, UriKind.Absolute, out var contentUri) &&
           string.Equals(manifestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(contentUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
           string.IsNullOrEmpty(manifestUri.UserInfo) &&
           string.IsNullOrEmpty(manifestUri.Query) &&
           string.IsNullOrEmpty(manifestUri.Fragment) &&
           string.IsNullOrEmpty(contentUri.UserInfo) &&
           string.IsNullOrEmpty(contentUri.Query) &&
           string.IsNullOrEmpty(contentUri.Fragment) &&
           contentUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal) &&
           !manifestUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) &&
           !contentUri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) &&
           !string.Equals(KeyId, "UNCONFIGURED", StringComparison.Ordinal) &&
           PublicKeyPem.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal);

    public static Uri ManifestUri
        => IsConfigured
            ? new Uri(ManifestUriText, UriKind.Absolute)
            : throw new InvalidOperationException("The trusted release channel is not configured.");

    public static Uri ContentBaseUri
        => IsConfigured
            ? new Uri(ContentBaseUriText, UriKind.Absolute)
            : throw new InvalidOperationException("The trusted release channel is not configured.");
}
