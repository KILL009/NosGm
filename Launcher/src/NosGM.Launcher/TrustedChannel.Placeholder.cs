// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal static class TrustedChannelConfiguration
{
    public const string ManifestUriText = "https://updates.example.invalid/nosgm/release-manifest.json";
    public const string ContentBaseUriText = "https://updates.example.invalid/nosgm/content/";
    public const string KeyId = "UNCONFIGURED";
    public static string PublicKeyPem { get; } = string.Empty;
}
