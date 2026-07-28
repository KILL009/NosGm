// SPDX-License-Identifier: MIT
// Patch-shape logic adapted from NosCoreIO/NosCore.DeveloperTools (MIT),
// Copyright (c) 2026 NosCoreIO. NosGM adds transactional output,
// ambiguity rejection and embedded-stub deployment.

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NosGM.Updater.Core;

namespace NosGM.Launcher;

internal sealed record SteamClientPreparation(string ExecutablePath, string StubPath);

internal static class SteamClientPatcher
{
    public const string PatchedExecutableName = "NostaleClientX_NosGM.exe";
    public const string StubFileName = "noscore_gf.dll";
    private const string StubResourceName = "NosGM.NosCoreGfStub";
    private static readonly byte[] OriginalImportName = Encoding.ASCII.GetBytes("gf_wrapper.dll\0");
    private static readonly byte[] ReplacementImportName = Encoding.ASCII.GetBytes("noscore_gf.dll\0");

    public static bool IsSteamInstallation(string installRoot)
    {
        if (string.IsNullOrWhiteSpace(installRoot))
        {
            return false;
        }

        var normalized = Path.GetFullPath(installRoot);
        return File.Exists(Path.Combine(normalized, "steam_api.dll")) ||
               File.Exists(Path.Combine(normalized, "steam_api64.dll")) ||
               normalized.Contains(
                   $"{Path.DirectorySeparatorChar}steamapps{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static SteamClientPreparation Prepare(
        string installRoot,
        string sourceExecutableName,
        string loginServerAddress)
    {
        var address = ValidateLoginAddress(loginServerAddress);
        var sourcePath = SafePaths.ResolveManagedPath(installRoot, sourceExecutableName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source NosTale client executable was not found.", sourcePath);
        }

        if (string.Equals(
                Path.GetFileName(sourcePath),
                PatchedExecutableName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Steam client patch must start from the original NostaleClientX.exe.");
        }

        var outputPath = SafePaths.ResolveManagedPath(installRoot, PatchedExecutableName);
        var stubPath = SafePaths.ResolveManagedPath(installRoot, StubFileName);
        var bytes = File.ReadAllBytes(sourcePath);

        PatchLoginAddress(bytes, address);
        PatchWrapperImport(bytes);
        WriteAtomic(outputPath, bytes);
        DeployStubAtomic(stubPath);

        return new SteamClientPreparation(outputPath, stubPath);
    }

    private static string ValidateLoginAddress(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length is < 7 or > 15 ||
            !IPAddress.TryParse(trimmed, out var address) ||
            address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new InvalidDataException(
                "Steam client preparation requires an IPv4 Login address of at most 15 characters.");
        }

        return address.ToString();
    }

    private static void PatchLoginAddress(byte[] bytes, string newAddress)
    {
        var candidates = FindIpShapedAnsiStrings(bytes);
        if (candidates.Count == 0)
        {
            throw new InvalidDataException(
                "The client does not contain the expected Delphi Login-address string.");
        }

        var distinctValues = candidates
            .Select(candidate => candidate.CurrentValue)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (distinctValues.Length != 1)
        {
            throw new InvalidDataException(
                "The client contains several different IP-shaped constants; refusing an ambiguous patch.");
        }

        var replacement = Encoding.ASCII.GetBytes(newAddress);
        foreach (var candidate in candidates)
        {
            if (replacement.Length > candidate.DeclaredLength)
            {
                throw new InvalidDataException(
                    "The configured Login address does not fit the client Login-address slot.");
            }

            replacement.CopyTo(bytes, candidate.PayloadOffset);
            Array.Clear(
                bytes,
                candidate.PayloadOffset + replacement.Length,
                candidate.DeclaredLength - replacement.Length);
            WriteInt32LittleEndian(bytes, candidate.PayloadOffset - 4, replacement.Length);
        }
    }

    private static List<IpCandidate> FindIpShapedAnsiStrings(byte[] bytes)
    {
        var candidates = new List<IpCandidate>();
        for (var index = 0; index <= bytes.Length - 20; index++)
        {
            if (bytes[index] != 0xFF ||
                bytes[index + 1] != 0xFF ||
                bytes[index + 2] != 0xFF ||
                bytes[index + 3] != 0xFF)
            {
                continue;
            }

            var declaredLength = BitConverter.ToInt32(bytes, index + 4);
            if (declaredLength is < 7 or > 15)
            {
                continue;
            }

            var payloadOffset = index + 8;
            if (payloadOffset + declaredLength > bytes.Length)
            {
                continue;
            }

            var end = payloadOffset + declaredLength;
            while (end > payloadOffset && bytes[end - 1] == 0)
            {
                end--;
            }

            var actualLength = end - payloadOffset;
            if (actualLength < 7)
            {
                continue;
            }

            var dotCount = 0;
            var valid = true;
            for (var cursor = payloadOffset; cursor < end; cursor++)
            {
                var current = bytes[cursor];
                if (current == '.')
                {
                    dotCount++;
                }
                else if (current is < (byte)'0' or > (byte)'9')
                {
                    valid = false;
                    break;
                }
            }

            if (!valid || dotCount != 3)
            {
                continue;
            }

            var currentValue = Encoding.ASCII.GetString(bytes, payloadOffset, actualLength);
            if (!IPAddress.TryParse(currentValue, out var parsed) ||
                parsed.AddressFamily != AddressFamily.InterNetwork)
            {
                continue;
            }

            candidates.Add(new IpCandidate(payloadOffset, declaredLength, currentValue));
        }

        return candidates;
    }

    private static void PatchWrapperImport(byte[] bytes)
    {
        if (OriginalImportName.Length != ReplacementImportName.Length)
        {
            throw new InvalidOperationException("Steam wrapper import names must have equal byte length.");
        }

        var originalOffsets = FindAll(bytes, OriginalImportName);
        var replacementOffsets = FindAll(bytes, ReplacementImportName);
        if (originalOffsets.Count == 0 && replacementOffsets.Count == 1)
        {
            return;
        }

        if (originalOffsets.Count != 1 || replacementOffsets.Count != 0)
        {
            throw new InvalidDataException(
                "The client wrapper import is missing, duplicated or already ambiguously modified.");
        }

        ReplacementImportName.CopyTo(bytes, originalOffsets[0]);
    }

    private static List<int> FindAll(byte[] haystack, byte[] needle)
    {
        var offsets = new List<int>();
        for (var index = 0; index <= haystack.Length - needle.Length; index++)
        {
            var matched = true;
            for (var needleIndex = 0; needleIndex < needle.Length; needleIndex++)
            {
                if (haystack[index + needleIndex] == needle[needleIndex])
                {
                    continue;
                }

                matched = false;
                break;
            }

            if (matched)
            {
                offsets.Add(index);
            }
        }

        return offsets;
    }

    private static void DeployStubAtomic(string destinationPath)
    {
        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream(StubResourceName)
            ?? throw new FileNotFoundException("The embedded Steam authentication stub is missing.");
        var temporaryPath = destinationPath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (var destination = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       81920,
                       FileOptions.WriteThrough))
            {
                source.CopyTo(destination);
                destination.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static void WriteAtomic(string destinationPath, byte[] bytes)
    {
        var temporaryPath = destinationPath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       81920,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static void WriteInt32LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private sealed record IpCandidate(int PayloadOffset, int DeclaredLength, string CurrentValue);
}
