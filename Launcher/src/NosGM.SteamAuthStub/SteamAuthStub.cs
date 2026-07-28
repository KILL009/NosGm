// SPDX-License-Identifier: MIT
// Adapted from NosCoreIO/NosCore.DeveloperTools (MIT), Copyright (c) 2026 NosCoreIO.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NosGM.SteamAuthStub;

internal static class SteamAuthStub
{
    private static readonly Dictionary<string, IntPtr> AsciiCache = new(StringComparer.Ordinal);
    private static byte[]? _authorizationCodeBytes;
    private static string _steamLanguage = "english";
    private static bool _initialized;

    [ModuleInitializer]
    internal static void OnLoad()
    {
        try
        {
            var authorizationCode = Environment.GetEnvironmentVariable("_NC_AUTH_CODE");
            var installationId = Environment.GetEnvironmentVariable("_NC_INSTALLATION_ID");
            var language = Environment.GetEnvironmentVariable("_NC_STEAM_LANGUAGE");

            if (!Guid.TryParse(authorizationCode, out var parsedAuthorizationCode) ||
                parsedAuthorizationCode == Guid.Empty ||
                !Guid.TryParse(installationId, out var parsedInstallationId) ||
                parsedInstallationId == Guid.Empty)
            {
                return;
            }

            _authorizationCodeBytes = Encoding.ASCII.GetBytes(parsedAuthorizationCode.ToString("D"));
            _steamLanguage = NormalizeLanguage(language);
            TrySeedInstallationId(parsedInstallationId.ToString("D"));
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_Init", CallConvs = [typeof(CallConvCdecl)])]
    public static int SteamInit() => _initialized ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "Steam_IsLoggedIn", CallConvs = [typeof(CallConvCdecl)])]
    public static int SteamIsLoggedIn() => _initialized ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "Steam_IsOverlayEnabled", CallConvs = [typeof(CallConvCdecl)])]
    public static int SteamIsOverlayEnabled() => 0;

    [UnmanagedCallersOnly(EntryPoint = "Steam_OnFrameTick", CallConvs = [typeof(CallConvCdecl)])]
    public static void SteamOnFrameTick()
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_SetOverlayActivateCallback", CallConvs = [typeof(CallConvCdecl)])]
    public static void SteamSetOverlayActivateCallback(IntPtr callback)
    {
        _ = callback;
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_OpenOverlayToWebPage", CallConvs = [typeof(CallConvCdecl)])]
    public static void SteamOpenOverlayToWebPage(IntPtr url)
    {
        _ = url;
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_CancelAuthSessionTicket", CallConvs = [typeof(CallConvCdecl)])]
    public static void SteamCancelAuthSessionTicket(uint ticketHandle)
    {
        _ = ticketHandle;
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_GetAuthSessionTicket", CallConvs = [typeof(CallConvCdecl)])]
    public static uint SteamGetAuthSessionTicket(IntPtr buffer, int bufferSize, IntPtr ticketSize)
    {
        if (!_initialized ||
            _authorizationCodeBytes is null ||
            buffer == IntPtr.Zero ||
            bufferSize < _authorizationCodeBytes.Length)
        {
            if (ticketSize != IntPtr.Zero)
            {
                Marshal.WriteInt32(ticketSize, 0);
            }

            return 0;
        }

        Marshal.Copy(_authorizationCodeBytes, 0, buffer, _authorizationCodeBytes.Length);
        if (ticketSize != IntPtr.Zero)
        {
            Marshal.WriteInt32(ticketSize, _authorizationCodeBytes.Length);
        }

        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "Steam_GetPersonaName", CallConvs = [typeof(CallConvCdecl)])]
    public static IntPtr SteamGetPersonaName() => PinAscii(string.Empty);

    [UnmanagedCallersOnly(EntryPoint = "Steam_GetSteamLanguage", CallConvs = [typeof(CallConvCdecl)])]
    public static IntPtr SteamGetSteamLanguage() => PinAscii(_steamLanguage);

    [UnmanagedCallersOnly(EntryPoint = "Steam_GetConnectedBetaBranch", CallConvs = [typeof(CallConvCdecl)])]
    public static IntPtr SteamGetConnectedBetaBranch() => PinAscii("live");

    private static IntPtr PinAscii(string value)
    {
        lock (AsciiCache)
        {
            if (AsciiCache.TryGetValue(value, out var existing))
            {
                return existing;
            }

            var bytes = Encoding.ASCII.GetBytes(value + "\0");
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            AsciiCache[value] = pointer;
            return pointer;
        }
    }

    private static string NormalizeLanguage(string? language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "german" => "german",
            "french" => "french",
            "italian" => "italian",
            "polish" => "polish",
            "spanish" => "spanish",
            "czech" => "czech",
            "russian" => "russian",
            "japanese" => "japanese",
            "schinese" => "schinese",
            _ => "english"
        };
    }

    private static void TrySeedInstallationId(string installationId)
    {
        TryWriteRegistryString(
            HkeyCurrentUser,
            @"SOFTWARE\Gameforge4d\TNTClient\MainApp",
            "InstallationId",
            installationId);
        TryWriteRegistryString(
            HkeyCurrentUser,
            @"SOFTWARE\Gameforge4d\GameforgeClient\MainApp",
            "InstallationId",
            installationId);
        TryWriteRegistryString(
            HkeyLocalMachine,
            @"SOFTWARE\WOW6432Node\Gameforge4d\GameforgeClient\MainApp",
            "InstallationId",
            installationId);
    }

    private static void TryWriteRegistryString(
        IntPtr hive,
        string keyPath,
        string valueName,
        string value)
    {
        if (RegCreateKeyExW(
                hive,
                keyPath,
                0,
                IntPtr.Zero,
                0,
                KeyWrite,
                IntPtr.Zero,
                out var key,
                out _) != 0)
        {
            return;
        }

        try
        {
            var bytes = Encoding.Unicode.GetBytes(value + "\0");
            _ = RegSetValueExW(key, valueName, 0, RegSz, bytes, (uint)bytes.Length);
        }
        finally
        {
            _ = RegCloseKey(key);
        }
    }

    private const uint KeyWrite = 0x20006;
    private const uint RegSz = 1;
    private static readonly IntPtr HkeyCurrentUser = (IntPtr)unchecked((int)0x80000001);
    private static readonly IntPtr HkeyLocalMachine = (IntPtr)unchecked((int)0x80000002);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegCreateKeyExW(
        IntPtr key,
        string subKey,
        int reserved,
        IntPtr classType,
        int options,
        uint desiredAccess,
        IntPtr securityAttributes,
        out IntPtr result,
        out int disposition);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegSetValueExW(
        IntPtr key,
        string valueName,
        int reserved,
        uint type,
        byte[] data,
        uint dataSize);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr key);
}
