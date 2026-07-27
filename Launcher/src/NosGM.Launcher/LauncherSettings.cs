// SPDX-License-Identifier: MIT

using System.IO;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed record LauncherSettings
{
    public int SchemaVersion { get; init; } = 1;
    public string InstallRoot { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Client");
    public string GameExecutable { get; init; } = "NostaleClientX.exe";
    public string Language { get; init; } = "es";
    public string AuthenticationEndpoint { get; init; } =
        Environment.GetEnvironmentVariable("NOSGM_AUTH_ENDPOINT") ?? string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public bool CloseAfterLaunch { get; init; }
}

internal static class LauncherSettingsStore
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "settings.json");

    public static async Task<LauncherSettings> LoadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaults = new LauncherSettings();
            await SaveAsync(defaults);
            return defaults;
        }

        var settings = await JsonSupport.ReadAsync<LauncherSettings>(SettingsPath);
        Validate(settings);
        return settings;
    }

    public static Task SaveAsync(LauncherSettings settings)
    {
        Validate(settings);
        return JsonSupport.WriteAtomicAsync(SettingsPath, settings);
    }

    private static void Validate(LauncherSettings settings)
    {
        if (settings.SchemaVersion != 1 ||
            string.IsNullOrWhiteSpace(settings.InstallRoot) ||
            !Path.IsPathFullyQualified(settings.InstallRoot) ||
            string.IsNullOrWhiteSpace(settings.GameExecutable) ||
            Path.GetFileName(settings.GameExecutable) != settings.GameExecutable ||
            settings.GameExecutable.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            settings.AccountName.Length > 255 ||
            settings.AccountName.IndexOfAny(['\t', '\r', '\n', '\v', '\0']) >= 0 ||
            !IsSafeAuthenticationEndpoint(settings.AuthenticationEndpoint))
        {
            throw new InvalidDataException("Launcher settings are invalid.");
        }

        var supportedLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "es", "en", "de", "fr", "it", "pl", "cz", "ru", "jp", "cn"
        };
        if (!supportedLanguages.Contains(settings.Language))
        {
            throw new InvalidDataException($"Unsupported launcher language '{settings.Language}'.");
        }
    }

    private static bool IsSafeAuthenticationEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return true;
        }

        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.Equals(uri.AbsolutePath, "/api/v1/launcher/ticket", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
    }
}
