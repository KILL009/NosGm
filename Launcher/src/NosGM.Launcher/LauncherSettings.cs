// SPDX-License-Identifier: MIT

using System.IO;
using System.Net;
using System.Net.Sockets;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed record LauncherSettings
{
    public const string OfficialDiscordApplicationId = "1534034979363754014";

    public int SchemaVersion { get; init; } = 1;
    public string InstallRoot { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Client");
    public string GameExecutable { get; init; } = "NostaleClientX.exe";
    public string Language { get; init; } = "es";
    public string AuthenticationEndpoint { get; init; } = string.Empty;
    public string AuthenticationTransport { get; init; } = "auto";
    public string LoginServerAddress { get; init; } = "127.0.0.1";
    public string PortalBaseUri { get; init; } = "http://localhost:5080/";
    public string AccountName { get; init; } = string.Empty;
    public string[] RecentAccountNames { get; init; } = Array.Empty<string>();
    public bool CloseAfterLaunch { get; init; }
    public bool DiscordRichPresenceEnabled { get; init; } = true;
    public string DiscordApplicationId { get; init; } = OfficialDiscordApplicationId;
    public bool DiscordShowCharacterName { get; init; } = true;
    public bool DiscordShowMap { get; init; } = true;
    public bool DiscordShowChannel { get; init; } = true;
    public bool DiscordShowParty { get; init; } = true;
}

internal static class LauncherSettingsStore
{
    private const string AuthenticationEndpointEnvironmentVariable = "NOSGM_AUTH_ENDPOINT";
    private const string AuthenticationTransportEnvironmentVariable = "NOSGM_LOGIN_TRANSPORT";
    private const string LoginServerAddressEnvironmentVariable = "NOSGM_LOGIN_ADDRESS";
    private const string PortalBaseUriEnvironmentVariable = "NOSGM_PORTAL_BASE_URI";
    private const string DiscordApplicationIdEnvironmentVariable = "NOSGM_DISCORD_APPLICATION_ID";

    private static string _persistedAuthenticationEndpoint = string.Empty;
    private static string _persistedAuthenticationTransport = "auto";
    private static string _persistedLoginServerAddress = "127.0.0.1";
    private static string _persistedPortalBaseUri = "http://localhost:5080/";
    private static string _persistedDiscordApplicationId = LauncherSettings.OfficialDiscordApplicationId;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NosGM",
        "Launcher",
        "settings.json");

    public static async Task<LauncherSettings> LoadAsync()
    {
        LauncherSettings persistedSettings;
        if (!File.Exists(SettingsPath))
        {
            persistedSettings = new LauncherSettings();
            Validate(persistedSettings);
            await JsonSupport.WriteAtomicAsync(SettingsPath, persistedSettings);
        }
        else
        {
            persistedSettings = await JsonSupport.ReadAsync<LauncherSettings>(SettingsPath);
            Validate(persistedSettings);

            // Migrate launchers that saved the pre-Rich-Presence empty value.
            // The Application ID is public application metadata, not a secret.
            if (string.IsNullOrWhiteSpace(persistedSettings.DiscordApplicationId))
            {
                persistedSettings = persistedSettings with
                {
                    DiscordApplicationId = LauncherSettings.OfficialDiscordApplicationId
                };
                await JsonSupport.WriteAtomicAsync(SettingsPath, persistedSettings);
            }
        }

        _persistedAuthenticationEndpoint = persistedSettings.AuthenticationEndpoint;
        _persistedAuthenticationTransport = persistedSettings.AuthenticationTransport;
        _persistedLoginServerAddress = persistedSettings.LoginServerAddress;
        _persistedPortalBaseUri = persistedSettings.PortalBaseUri;
        _persistedDiscordApplicationId = persistedSettings.DiscordApplicationId;

        var effectiveSettings = persistedSettings with
        {
            AuthenticationEndpoint = GetRuntimeValue(AuthenticationEndpointEnvironmentVariable) ??
                                     persistedSettings.AuthenticationEndpoint,
            AuthenticationTransport = GetRuntimeValue(AuthenticationTransportEnvironmentVariable) ??
                                      persistedSettings.AuthenticationTransport,
            LoginServerAddress = GetRuntimeValue(LoginServerAddressEnvironmentVariable) ??
                                 persistedSettings.LoginServerAddress,
            PortalBaseUri = GetRuntimeValue(PortalBaseUriEnvironmentVariable) ??
                            persistedSettings.PortalBaseUri,
            DiscordApplicationId = GetRuntimeValue(DiscordApplicationIdEnvironmentVariable) ??
                                   persistedSettings.DiscordApplicationId
        };
        Validate(effectiveSettings);
        return effectiveSettings;
    }

    public static Task SaveAsync(LauncherSettings settings)
    {
        var persistedSettings = settings with
        {
            AuthenticationEndpoint = GetRuntimeValue(AuthenticationEndpointEnvironmentVariable) is null
                ? settings.AuthenticationEndpoint
                : _persistedAuthenticationEndpoint,
            AuthenticationTransport = GetRuntimeValue(AuthenticationTransportEnvironmentVariable) is null
                ? settings.AuthenticationTransport
                : _persistedAuthenticationTransport,
            LoginServerAddress = GetRuntimeValue(LoginServerAddressEnvironmentVariable) is null
                ? settings.LoginServerAddress
                : _persistedLoginServerAddress,
            PortalBaseUri = GetRuntimeValue(PortalBaseUriEnvironmentVariable) is null
                ? settings.PortalBaseUri
                : _persistedPortalBaseUri,
            DiscordApplicationId = GetRuntimeValue(DiscordApplicationIdEnvironmentVariable) is null
                ? settings.DiscordApplicationId
                : _persistedDiscordApplicationId
        };
        Validate(persistedSettings);
        _persistedAuthenticationEndpoint = persistedSettings.AuthenticationEndpoint;
        _persistedAuthenticationTransport = persistedSettings.AuthenticationTransport;
        _persistedLoginServerAddress = persistedSettings.LoginServerAddress;
        _persistedPortalBaseUri = persistedSettings.PortalBaseUri;
        _persistedDiscordApplicationId = persistedSettings.DiscordApplicationId;
        return JsonSupport.WriteAtomicAsync(SettingsPath, persistedSettings);
    }

    private static string? GetRuntimeValue(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void Validate(LauncherSettings settings)
    {
        if (settings.SchemaVersion != 1 ||
            string.IsNullOrWhiteSpace(settings.InstallRoot) ||
            !Path.IsPathFullyQualified(settings.InstallRoot) ||
            string.IsNullOrWhiteSpace(settings.GameExecutable) ||
            Path.GetFileName(settings.GameExecutable) != settings.GameExecutable ||
            settings.GameExecutable.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !IsStoredAccountName(settings.AccountName, allowEmpty: true) ||
            !AreRecentAccountsValid(settings.RecentAccountNames) ||
            !IsSafeAuthenticationEndpoint(settings.AuthenticationEndpoint) ||
            !IsSupportedTransport(settings.AuthenticationTransport) ||
            !IsIpv4Address(settings.LoginServerAddress) ||
            !IsSafePortalBaseUri(settings.PortalBaseUri) ||
            !IsDiscordApplicationId(settings.DiscordApplicationId))
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

    private static bool AreRecentAccountsValid(string[]? values)
    {
        if (values is null || values.Length > LauncherAccountHistory.MaximumRecentAccounts)
        {
            return false;
        }

        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (!IsStoredAccountName(value, allowEmpty: false) || !distinct.Add(value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStoredAccountName(string? value, bool allowEmpty)
    {
        if (value is null)
        {
            return false;
        }

        if (value.Length == 0)
        {
            return allowEmpty;
        }

        return value.Length <= 255 &&
               string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
               value.IndexOfAny(['\t', '\r', '\n', '\v', '\0']) < 0 &&
               !value.Any(char.IsControl);
    }

    private static bool IsSupportedTransport(string value)
    {
        return string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "gameforge-pipe", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "steam-stub", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIpv4Address(string value)
    {
        return IPAddress.TryParse(value?.Trim(), out var address) &&
               address.AddressFamily == AddressFamily.InterNetwork;
    }

    private static bool IsDiscordApplicationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length is >= 15 and <= 22 &&
               normalized.All(char.IsDigit);
    }

    private static bool IsSafePortalBaseUri(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback;
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
