// SPDX-License-Identifier: MIT

namespace NosGM.ClientThemeEditor;

internal static class ThemeEngine
{
    public static PatchPlan BuildPlan(
        byte[] content,
        ClientIdentity identity,
        ThemeProfile profile,
        ThemeDocument theme)
    {
        ValidateIdentity(identity, profile);
        ValidateProfile(profile);

        var operations = new List<PlannedPatch>();
        var occupied = new List<(int Start, int End, string Id)>();

        foreach (var patch in profile.Patches.Where(item => item.Enabled).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!theme.Colors.TryGetValue(patch.Id, out var color))
            {
                continue;
            }

            var pattern = HexCodec.ParsePattern(patch.PatternHex);
            var matches = PatternMatcher.FindAll(content, pattern);
            if (matches.Count != patch.ExpectedMatches)
            {
                throw new InvalidDataException(
                    $"Patch '{patch.Id}' expected {patch.ExpectedMatches} pattern match(es), but found {matches.Count}.");
            }

            var original = HexCodec.ParseExact(patch.ExpectedOriginalHex, $"{patch.Id}.expectedOriginalHex");
            var replacement = HexCodec.EncodeColor(color, patch.ColorEncoding);
            if (replacement.Length != original.Length)
            {
                throw new InvalidDataException(
                    $"Patch '{patch.Id}' encodes {replacement.Length} bytes but expects {original.Length} original bytes.");
            }

            foreach (var match in matches)
            {
                var valueOffset = checked(match + patch.ValueOffset);
                var end = checked(valueOffset + original.Length);
                if (valueOffset < 0 || end > content.Length)
                {
                    throw new InvalidDataException($"Patch '{patch.Id}' points outside the client file.");
                }

                if (!content.AsSpan(valueOffset, original.Length).SequenceEqual(original))
                {
                    throw new InvalidDataException(
                        $"Patch '{patch.Id}' original bytes do not match at 0x{valueOffset:X}.");
                }

                var overlap = occupied.FirstOrDefault(range => valueOffset < range.End && end > range.Start);
                if (overlap != default)
                {
                    throw new InvalidDataException(
                        $"Patch '{patch.Id}' overlaps patch '{overlap.Id}' at 0x{valueOffset:X}.");
                }

                occupied.Add((valueOffset, end, patch.Id));
                operations.Add(new PlannedPatch(
                    patch.Id,
                    patch.Description,
                    match,
                    valueOffset,
                    HexCodec.Format(original),
                    HexCodec.Format(replacement)));
            }
        }

        if (operations.Count == 0)
        {
            throw new InvalidDataException("No enabled profile patch matched a color in the selected theme.");
        }

        return new PatchPlan(
            identity,
            profile.ProfileName,
            theme.ThemeName,
            operations.OrderBy(item => item.ValueOffset).ThenBy(item => item.Id, StringComparer.Ordinal).ToArray());
    }

    public static byte[] Apply(byte[] original, PatchPlan plan)
    {
        var result = original.ToArray();
        foreach (var operation in plan.Operations)
        {
            var expected = HexCodec.ParseExact(operation.OriginalHex, $"{operation.Id}.originalHex");
            var replacement = HexCodec.ParseExact(operation.ReplacementHex, $"{operation.Id}.replacementHex");
            if (!result.AsSpan(operation.ValueOffset, expected.Length).SequenceEqual(expected))
            {
                throw new InvalidDataException($"Patch '{operation.Id}' changed before application.");
            }

            replacement.CopyTo(result.AsSpan(operation.ValueOffset, replacement.Length));
        }

        return result;
    }

    public static PatchManifest ApplyToOutput(
        string inputPath,
        string outputPath,
        ThemeProfile profile,
        ThemeDocument theme,
        bool overwrite)
    {
        var input = Path.GetFullPath(inputPath);
        var output = Path.GetFullPath(outputPath);
        if (string.Equals(input, output, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Output path must differ from input. Use --in-place for guarded replacement.");
        }

        if (File.Exists(output) && !overwrite)
        {
            throw new IOException($"Output '{output}' already exists. Use --force to replace it.");
        }

        var original = File.ReadAllBytes(input);
        var identity = PeInspector.Inspect(input);
        var plan = BuildPlan(original, identity, profile, theme);
        var patched = Apply(original, plan);

        WriteAtomically(output, patched, overwrite);
        var patchedIdentity = PeInspector.Inspect(output);
        var manifest = CreateManifest(input, output, null, identity.Sha256, patchedIdentity.Sha256, plan);
        JsonFiles.Write(output + ".nosgm-theme-manifest.json", manifest);
        return manifest;
    }

    public static PatchManifest ApplyInPlace(
        string inputPath,
        ThemeProfile profile,
        ThemeDocument theme)
    {
        var input = Path.GetFullPath(inputPath);
        var original = File.ReadAllBytes(input);
        var identity = PeInspector.Inspect(input);
        var plan = BuildPlan(original, identity, profile, theme);
        var patched = Apply(original, plan);

        var root = Path.Combine(
            Path.GetDirectoryName(input)!,
            "NosGM.ThemeBackups",
            $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{identity.Sha256[..12]}");
        Directory.CreateDirectory(root);

        var backup = Path.Combine(root, Path.GetFileName(input));
        File.Copy(input, backup, overwrite: false);
        if (!string.Equals(PeInspector.Inspect(backup).Sha256, identity.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Backup hash does not match the original client.");
        }

        WriteAtomically(input, patched, overwrite: true);
        var patchedIdentity = PeInspector.Inspect(input);
        var manifest = CreateManifest(input, input, backup, identity.Sha256, patchedIdentity.Sha256, plan);
        JsonFiles.Write(Path.Combine(root, "manifest.json"), manifest);
        return manifest;
    }

    public static void Restore(string manifestPath)
    {
        var manifest = JsonFiles.Read<PatchManifest>(manifestPath);
        if (string.IsNullOrWhiteSpace(manifest.BackupPath))
        {
            throw new InvalidDataException("Manifest does not contain an in-place backup.");
        }

        var patchedPath = Path.GetFullPath(manifest.PatchedPath);
        var backupPath = Path.GetFullPath(manifest.BackupPath);
        if (!File.Exists(patchedPath) || !File.Exists(backupPath))
        {
            throw new FileNotFoundException("Patched file or backup is missing.");
        }

        var currentSha = PeInspector.Inspect(patchedPath).Sha256;
        if (!string.Equals(currentSha, manifest.PatchedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Current client hash differs from the manifest; restore refused.");
        }

        var backupSha = PeInspector.Inspect(backupPath).Sha256;
        if (!string.Equals(backupSha, manifest.OriginalSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Backup hash differs from the manifest; restore refused.");
        }

        var bytes = File.ReadAllBytes(backupPath);
        WriteAtomically(patchedPath, bytes, overwrite: true);
        var restoredSha = PeInspector.Inspect(patchedPath).Sha256;
        if (!string.Equals(restoredSha, manifest.OriginalSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Restored client hash does not match the original manifest.");
        }
    }

    private static PatchManifest CreateManifest(
        string originalPath,
        string patchedPath,
        string? backupPath,
        string originalSha,
        string patchedSha,
        PatchPlan plan)
        => new()
        {
            ProfileName = plan.ProfileName,
            ThemeName = plan.ThemeName,
            OriginalPath = originalPath,
            PatchedPath = patchedPath,
            BackupPath = backupPath,
            OriginalSha256 = originalSha,
            PatchedSha256 = patchedSha,
            Operations = plan.Operations
        };

    private static void ValidateIdentity(ClientIdentity identity, ThemeProfile profile)
    {
        if (profile.ResearchOnly)
        {
            throw new InvalidDataException("Research-only profiles cannot be applied.");
        }

        if (!string.Equals(identity.FileName, profile.ExpectedFileName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(identity.Architecture, profile.ExpectedArchitecture, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(identity.FileVersion, profile.ExpectedFileVersion, StringComparison.Ordinal) ||
            identity.Length != profile.ExpectedLength ||
            !string.Equals(identity.Sha256, profile.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Client identity does not exactly match the selected profile.");
        }
    }

    private static void ValidateProfile(ThemeProfile profile)
    {
        if (profile.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported profile schema {profile.SchemaVersion}.");
        }

        if (!string.Equals(profile.ExpectedArchitecture, "x86", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only exact x86 client profiles are supported.");
        }

        if (profile.ExpectedSha256.Length != 64 ||
            profile.ExpectedSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException("Profile SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        var duplicate = profile.Patches
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Profile contains duplicate patch id '{duplicate.Key}'.");
        }

        foreach (var patch in profile.Patches)
        {
            if (string.IsNullOrWhiteSpace(patch.Id))
            {
                throw new InvalidDataException("Patch id cannot be empty.");
            }

            if (patch.ExpectedMatches < 1 || patch.ExpectedMatches > 16)
            {
                throw new InvalidDataException($"Patch '{patch.Id}' expectedMatches must be between 1 and 16.");
            }
        }
    }

    private static void WriteAtomically(string destinationPath, byte[] bytes, bool overwrite)
    {
        var destination = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + $".tmp.{Guid.NewGuid():N}";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (new FileInfo(temporary).Length != bytes.Length)
            {
                throw new IOException("Temporary output length is invalid.");
            }

            File.Move(temporary, destination, overwrite);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
