// SPDX-License-Identifier: MIT

using System.IO;
using System.Net;
using System.Net.Sockets;
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
    public string AuthenticationEndpoint { get; init; } = string.Empty;
    public string AuthenticationTransport { get; init; } = "auto";
    public string LoginServerAddress { get; init; } = "127.0.0.1";
    public string AccountName { get; init; } = string.Empty;
    public bool CloseAfterLaunch { get; init; }
}

internal static class LauncherSettingsStore
{
    private const string AuthenticationEndpointEnvironmentVariable = "NOSGM_AUTH_ENDPOINT";
    private const string AuthenticationTransportEnvironmentVariable = "NOSGM_LOGIN_TRANSPORT";
    private const string LoginServerAddressEnvironmentVariable = "NOSGM_LOGIN_ADDRESS";

    private static string _persistedAuthenticationEndpoint = string.Empty;
    private static string _persistedAuthenticationTransport = "auto";
    private static string _persistedLoginServerAddress = "127.0.0.1";

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
        }

        _persistedAuthenticationEndpoint = persistedSettings.AuthenticationEndpoint;
        _persistedAuthenticationTransport = persistedSettings.AuthenticationTransport;
        _persistedLoginServerAddress = persistedSettings.LoginServerAddress;

        var effectiveSettings = persistedSettings with
        {
            AuthenticationEndpoint = GetRuntimeValue(AuthenticationEndpointEnvironmentVariable) ??
                                     persistedSettings.AuthenticationEndpoint,
            AuthenticationTransport = GetRuntimeValue(AuthenticationTransportEnvironmentVariable) ??
                                      persistedSettings.AuthenticationTransport,
            LoginServerAddress = GetRuntimeValue(LoginServerAddressEnvironmentVariable) ??
                                 persistedSettings.LoginServerAddress
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
                : _persistedLoginServerAddress
        };
        Validate(persistedSettings);
        _persistedAuthenticationEndpoint = persistedSettings.AuthenticationEndpoint;
        _persistedAuthenticationTransport = persistedSettings.AuthenticationTransport;
        _persistedLoginServerAddress = persistedSettings.LoginServerAddress;
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
            settings.AccountName.Length > 255 ||
            settings.AccountName.IndexOfAny(['\t', '\r', '\n', '\v', '\0']) >= 0 ||
            !IsSafeAuthenticationEndpoint(settings.AuthenticationEndpoint) ||
            !IsSupportedTransport(settings.AuthenticationTransport) ||
            !IsIpv4Address(settings.LoginServerAddress))
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
