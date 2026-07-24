// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;

namespace NosGM.Web;

public sealed class PublicDataOptions
{
    public const string SectionName = "PublicData";

    [Required, StringLength(4096, MinimumLength = 5)]
    public string SnapshotPath { get; init; } = "App_Data/public-snapshot.json";

    [Required, RegularExpression(@"^[0-9A-Za-z._-]{1,64}$")]
    public string KeyId { get; init; } = "nosgm-live-v1";

    [StringLength(4096)]
    public string HmacKeyBase64 { get; init; } = string.Empty;

    [Range(30, 3600)]
    public int MaximumAgeSeconds { get; init; } = 180;

    [Range(65_536, 4_194_304)]
    public int MaximumSnapshotBytes { get; init; } = 1_048_576;

    public string ResolveSnapshotPath(string contentRootPath)
    {
        var configured = Environment.ExpandEnvironmentVariables(SnapshotPath.Trim());
        return Path.GetFullPath(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(contentRootPath, configured));
    }

    public bool TryGetHmacKey(out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(HmacKeyBase64))
        {
            return false;
        }

        try
        {
            key = Convert.FromBase64String(HmacKeyBase64.Trim());
            return key.Length >= 32;
        }
        catch (FormatException)
        {
            key = [];
            return false;
        }
    }

    public static bool IsSafe(PublicDataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SnapshotPath)
            || options.SnapshotPath.IndexOf('\0') >= 0
            || !options.SnapshotPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(options.HmacKeyBase64)
               || options.TryGetHmacKey(out _);
    }
}
