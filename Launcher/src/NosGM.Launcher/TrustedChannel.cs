// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal static class TrustedChannel
{
    // Release builds must replace these placeholders with an HTTPS channel and
    // the public half of an offline-generated ECDSA P-256 release key.
    public const string ManifestUriText = "https://updates.example.invalid/nosgm/release-manifest.json";
    public const string ContentBaseUriText = "https://updates.example.invalid/nosgm/content/";
    public const string KeyId = "UNCONFIGURED";
    public const string PublicKeyPem = "";

    public static bool IsConfigured
        => Uri.TryCreate(ManifestUriText, UriKind.Absolute, out var manifestUri) &&
           Uri.TryCreate(ContentBaseUriText, UriKind.Absolute, out var contentUri) &&
           string.Equals(manifestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(contentUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
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
