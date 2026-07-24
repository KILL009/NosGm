// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;

namespace NosGM.Web;

public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    [Required, StringLength(40, MinimumLength = 2)]
    public string ServerName { get; init; } = "NosGM";

    [Required, RegularExpression(@"^[0-9A-Za-z._-]{1,32}$")]
    public string ClientVersion { get; init; } = "0.9.3.3255";

    [StringLength(2048)]
    public string LauncherDownloadUrl { get; init; } = string.Empty;

    public bool IsLauncherDownloadAvailable
        => Uri.TryCreate(LauncherDownloadUrl, UriKind.Absolute, out var uri)
           && uri.Scheme == Uri.UriSchemeHttps
           && !uri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);

    public static bool IsSafe(PortalOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LauncherDownloadUrl))
        {
            return true;
        }

        return Uri.TryCreate(options.LauncherDownloadUrl, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && string.IsNullOrEmpty(uri.UserInfo)
               && uri.Fragment.Length == 0;
    }
}
