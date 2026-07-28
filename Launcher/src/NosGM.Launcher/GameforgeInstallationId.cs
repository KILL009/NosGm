// SPDX-License-Identifier: MIT

using Microsoft.Win32;

namespace NosGM.Launcher;

internal static class GameforgeInstallationId
{
    private const string TntRegistryPath = @"Software\Gameforge4d\TNTClient\MainApp";
    private const string SteamRegistryPath = @"Software\Gameforge4d\GameforgeClient\MainApp";
    private const string ValueName = "InstallationId";

    public static string Resolve()
    {
        var installationId = TryReadCurrentUserTnt() ??
                             TryReadSteamMachine() ??
                             TryReadCurrentUserSteam() ??
                             Guid.NewGuid();

        WriteCurrentUserValue(TntRegistryPath, installationId);
        WriteCurrentUserValue(SteamRegistryPath, installationId);
        return installationId.ToString("D");
    }

    public static void EnsureSteamClientIdentity(string installationId)
    {
        if (!Guid.TryParse(installationId, out var parsed) || parsed == Guid.Empty)
        {
            throw new InvalidDataException("The Steam client InstallationId is invalid.");
        }

        WriteCurrentUserValue(TntRegistryPath, parsed);
        WriteCurrentUserValue(SteamRegistryPath, parsed);

        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        var existing = TryRead(machine, SteamRegistryPath);
        if (existing == parsed)
        {
            return;
        }

        try
        {
            using var key = machine.CreateSubKey(SteamRegistryPath, writable: true)
                ?? throw new InvalidOperationException(
                    "Windows could not create the Steam client identity registry key.");
            key.SetValue(ValueName, parsed.ToString("D"), RegistryValueKind.String);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException(
                "Steam client preparation needs one elevated launcher run to synchronize its InstallationId.",
                exception);
        }
    }

    private static Guid? TryReadCurrentUserTnt()
    {
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        return TryRead(currentUser, TntRegistryPath);
    }

    private static Guid? TryReadCurrentUserSteam()
    {
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
        return TryRead(currentUser, SteamRegistryPath);
    }

    private static Guid? TryReadSteamMachine()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        return TryRead(machine, SteamRegistryPath);
    }

    private static Guid? TryRead(RegistryKey baseKey, string path)
    {
        using var key = baseKey.OpenSubKey(path, writable: false);
        if (key?.GetValue(ValueName) is not string value ||
            !Guid.TryParse(value, out var parsed) ||
            parsed == Guid.Empty)
        {
            return null;
        }

        return parsed;
    }

    private static void WriteCurrentUserValue(string path, Guid installationId)
    {
        using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
        using var key = currentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException(
                "Windows could not open the current-user Gameforge identity registry key.");
        key.SetValue(ValueName, installationId.ToString("D"), RegistryValueKind.String);
    }
}
