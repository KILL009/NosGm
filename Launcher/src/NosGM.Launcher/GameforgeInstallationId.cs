// SPDX-License-Identifier: MIT

using Microsoft.Win32;

namespace NosGM.Launcher;

internal static class GameforgeInstallationId
{
    private const string RegistryPath = @"Software\Gameforge4d\TNTClient\MainApp";
    private const string ValueName = "InstallationId";

    public static string Resolve()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true)
            ?? throw new InvalidOperationException(
                "Windows could not open the Gameforge installation identity registry key.");

        if (key.GetValue(ValueName) is string existingValue &&
            !string.IsNullOrWhiteSpace(existingValue))
        {
            if (!Guid.TryParse(existingValue, out var existingId) || existingId == Guid.Empty)
            {
                throw new InvalidDataException(
                    "The Gameforge InstallationId stored in the current-user registry is invalid.");
            }

            return existingId.ToString("D");
        }

        var generatedId = Guid.NewGuid().ToString("D");
        key.SetValue(ValueName, generatedId, RegistryValueKind.String);
        return generatedId;
    }
}
