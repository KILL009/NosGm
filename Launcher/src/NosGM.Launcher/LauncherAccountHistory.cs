// SPDX-License-Identifier: MIT

namespace NosGM.Launcher;

internal static class LauncherAccountHistory
{
    public const int MaximumRecentAccounts = 5;

    public static LauncherSettings Normalize(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var current = NormalizeAccountName(settings.AccountName, allowEmpty: true);
        var ordered = new List<string>(MaximumRecentAccounts);
        AddDistinct(ordered, current);
        foreach (var accountName in settings.RecentAccountNames ?? Array.Empty<string>())
        {
            AddDistinct(ordered, NormalizeAccountName(accountName, allowEmpty: false));
        }

        return settings with
        {
            AccountName = current,
            RecentAccountNames = ordered.Take(MaximumRecentAccounts).ToArray()
        };
    }

    public static LauncherSettings Remember(LauncherSettings settings, string accountName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = NormalizeAccountName(accountName, allowEmpty: false);
        var ordered = new List<string>(MaximumRecentAccounts) { normalized };
        foreach (var existing in settings.RecentAccountNames ?? Array.Empty<string>())
        {
            AddDistinct(ordered, NormalizeAccountName(existing, allowEmpty: false));
        }

        AddDistinct(ordered, NormalizeAccountName(settings.AccountName, allowEmpty: true));
        return settings with
        {
            AccountName = normalized,
            RecentAccountNames = ordered.Take(MaximumRecentAccounts).ToArray()
        };
    }

    public static LauncherSettings Select(LauncherSettings settings, string accountName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = NormalizeAccountName(accountName, allowEmpty: false);
        if (!(settings.RecentAccountNames ?? Array.Empty<string>()).Contains(
                normalized,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected account is not in the launcher history.");
        }

        return Remember(settings, normalized);
    }

    public static LauncherSettings UseAnotherAccount(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings with { AccountName = string.Empty };
    }

    public static LauncherSettings Forget(LauncherSettings settings, string accountName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = NormalizeAccountName(accountName, allowEmpty: false);
        var remaining = (settings.RecentAccountNames ?? Array.Empty<string>())
            .Where(existing => !string.Equals(
                existing,
                normalized,
                StringComparison.OrdinalIgnoreCase))
            .Take(MaximumRecentAccounts)
            .ToArray();

        var current = string.Equals(
            settings.AccountName,
            normalized,
            StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : settings.AccountName;
        return settings with
        {
            AccountName = current,
            RecentAccountNames = remaining
        };
    }

    public static bool StoredAccountsEqual(LauncherSettings left, LauncherSettings right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return string.Equals(left.AccountName, right.AccountName, StringComparison.Ordinal) &&
               (left.RecentAccountNames ?? Array.Empty<string>()).SequenceEqual(
                   right.RecentAccountNames ?? Array.Empty<string>(),
                   StringComparer.Ordinal);
    }

    private static string NormalizeAccountName(string? accountName, bool allowEmpty)
    {
        var normalized = accountName?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            if (allowEmpty)
            {
                return string.Empty;
            }

            throw new InvalidDataException("Account name cannot be empty.");
        }

        if (normalized.Length > 255 ||
            normalized.IndexOfAny(['\t', '\r', '\n', '\v', '\0']) >= 0 ||
            normalized.Any(char.IsControl))
        {
            throw new InvalidDataException("Account name contains unsupported characters.");
        }

        return normalized;
    }

    private static void AddDistinct(List<string> values, string candidate)
    {
        if (candidate.Length == 0 ||
            values.Any(existing => string.Equals(
                existing,
                candidate,
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        values.Add(candidate);
    }
}
