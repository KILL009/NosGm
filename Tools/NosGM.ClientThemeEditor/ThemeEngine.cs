// SPDX-License-Identifier: MIT

using System.Security.Cryptography;

namespace NosGM.ClientThemeEditor;

internal static class ThemeEngine
{
    public static PatchPlan BuildPlan(
        byte[] content,
        ClientIdentity identity,
        ThemeProfile profile,
        ThemeDocument theme)
    {
        ValidateProfile(profile);
        ValidateIdentity(identity, profile);
        ValidateContentIdentity(content, identity);
        var colors = ValidateTheme(theme, profile);

        var operations = new List<PlannedPatch>();
        var occupied = new List<(int Start, int End, string Id)>();

        foreach (var patch in profile.Patches.Where(item => item.Enabled).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!colors.TryGetValue(patch.Id, out var color))
            {
                continue;
            }

            var pattern = HexCodec.ParsePattern(patch.PatternHex);
            var matches = PatternMatcher.FindAll(content, pattern, patch.ExpectedMatches + 1);
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
                int valueOffset;
                int end;
                try
                {
                    valueOffset = checked(match + patch.ValueOffset);
                    end = checked(valueOffset + original.Length);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidDataException($"Patch '{patch.Id}' offset arithmetic overflowed.", exception);
                }

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
                    patch.Description ?? string.Empty,
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
        if (PathsEqual(input, output))
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
        var expectedPatchedSha = Convert.ToHexString(SHA256.HashData(patched));

        var patchedIdentity = WritePatchedAtomically(
            output,
            patched,
            overwrite,
            preserveExistingOnFailure: true,
            identity,
            expectedPatchedSha);

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
        var expectedPatchedSha = Convert.ToHexString(SHA256.HashData(patched));

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

        if (!string.Equals(PeInspector.Inspect(input).Sha256, identity.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Client changed after backup verification; in-place patch refused.");
        }

        var manifestPath = Path.Combine(root, "manifest.json");
        var manifest = CreateManifest(input, input, backup, identity.Sha256, expectedPatchedSha, plan);
        JsonFiles.Write(manifestPath, manifest);

        try
        {
            var patchedIdentity = WritePatchedAtomically(
                input,
                patched,
                overwrite: true,
                preserveExistingOnFailure: false,
                identity,
                expectedPatchedSha);
            return manifest with { PatchedSha256 = patchedIdentity.Sha256 };
        }
        catch
        {
            WriteAtomically(input, original, overwrite: true);
            if (!string.Equals(PeInspector.Inspect(input).Sha256, identity.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Patch validation failed and automatic rollback could not restore the original client.");
            }

            File.Delete(manifestPath);
            throw;
        }
    }

    public static void Restore(string manifestPath)
    {
        var manifest = JsonFiles.Read<PatchManifest>(manifestPath);
        ValidateManifest(manifest);

        var patchedPath = Path.GetFullPath(manifest.PatchedPath);
        var backupPath = Path.GetFullPath(manifest.BackupPath!);
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

    private static void ValidateProfile(ThemeProfile profile)
    {
        if (profile.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported profile schema {profile.SchemaVersion}.");
        }

        if (profile.ResearchOnly)
        {
            throw new InvalidDataException("Research-only profiles cannot be planned or applied.");
        }

        if (string.IsNullOrWhiteSpace(profile.ProfileName) ||
            string.IsNullOrWhiteSpace(profile.ExpectedFileName))
        {
            throw new InvalidDataException("Profile name and expected file name are required.");
        }

        if (!string.Equals(profile.ExpectedArchitecture, "x86", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only exact x86 client profiles are supported.");
        }

        if (profile.ExpectedLength < 128)
        {
            throw new InvalidDataException("Profile expected length is too small to be a PE executable.");
        }

        if (string.IsNullOrWhiteSpace(profile.ExpectedSha256) ||
            profile.ExpectedSha256.Length != 64 ||
            profile.ExpectedSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException("Profile SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        if (profile.Patches is null)
        {
            throw new InvalidDataException("Profile patches cannot be null.");
        }

        if (profile.Patches.Count > 128)
        {
            throw new InvalidDataException("Profile cannot contain more than 128 patch definitions.");
        }

        for (var index = 0; index < profile.Patches.Count; index++)
        {
            if (profile.Patches[index] is null)
            {
                throw new InvalidDataException($"Profile patch at index {index} is null.");
            }
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
            if (string.IsNullOrWhiteSpace(patch.Id) ||
                string.IsNullOrWhiteSpace(patch.PatternHex) ||
                string.IsNullOrWhiteSpace(patch.ExpectedOriginalHex) ||
                string.IsNullOrWhiteSpace(patch.ColorEncoding))
            {
                throw new InvalidDataException("Patch id, pattern, original bytes and color encoding are required.");
            }

            if (patch.ExpectedMatches < 1 || patch.ExpectedMatches > 16)
            {
                throw new InvalidDataException($"Patch '{patch.Id}' expectedMatches must be between 1 and 16.");
            }
        }
    }

    private static Dictionary<string, string> ValidateTheme(ThemeDocument theme, ThemeProfile profile)
    {
        if (theme.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported theme schema {theme.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(theme.ThemeName) || theme.Colors is null || theme.Colors.Count == 0)
        {
            throw new InvalidDataException("Theme name and at least one color are required.");
        }

        var duplicate = theme.Colors.Keys
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Theme contains duplicate color id '{duplicate.Key}'.");
        }

        var enabledIds = profile.Patches
            .Where(item => item.Enabled)
            .Select(item => item.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in theme.Colors)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                throw new InvalidDataException("Theme color ids and values cannot be empty.");
            }

            if (!enabledIds.Contains(pair.Key))
            {
                throw new InvalidDataException(
                    $"Theme color '{pair.Key}' does not have an enabled definition in profile '{profile.ProfileName}'.");
            }

            colors.Add(pair.Key, pair.Value);
        }

        return colors;
    }

    private static void ValidateIdentity(ClientIdentity identity, ThemeProfile profile)
    {
        if (!string.Equals(identity.FileName, profile.ExpectedFileName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(identity.Architecture, profile.ExpectedArchitecture, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(identity.FileVersion, profile.ExpectedFileVersion, StringComparison.Ordinal) ||
            identity.Length != profile.ExpectedLength ||
            !string.Equals(identity.Sha256, profile.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Client identity does not exactly match the selected profile.");
        }
    }

    private static void ValidateContentIdentity(byte[] content, ClientIdentity identity)
    {
        if (content.LongLength != identity.Length)
        {
            throw new InvalidDataException("Client changed while it was being inspected.");
        }

        var contentSha = Convert.ToHexString(SHA256.HashData(content));
        if (!string.Equals(contentSha, identity.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Client content hash differs from the inspected identity.");
        }
    }

    private static void ValidatePatchedIdentity(
        ClientIdentity original,
        ClientIdentity patched,
        string expectedPatchedSha)
    {
        if (!string.Equals(patched.Architecture, original.Architecture, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(patched.FileVersion, original.FileVersion, StringComparison.Ordinal) ||
            patched.Length != original.Length ||
            !string.Equals(patched.Sha256, expectedPatchedSha, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Patched output failed post-write PE identity validation.");
        }
    }

    private static void ValidateManifest(PatchManifest manifest)
    {
        if (manifest.SchemaVersion != 1 ||
            string.IsNullOrWhiteSpace(manifest.PatchedPath) ||
            string.IsNullOrWhiteSpace(manifest.BackupPath) ||
            !IsSha256(manifest.OriginalSha256) ||
            !IsSha256(manifest.PatchedSha256))
        {
            throw new InvalidDataException("Manifest is incomplete or unsupported.");
        }
    }

    private static bool IsSha256(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Length == 64 &&
           value.All(Uri.IsHexDigit);

    private static bool PathsEqual(string left, string right)
        => string.Equals(
            left,
            right,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static ClientIdentity WritePatchedAtomically(
        string destinationPath,
        byte[] bytes,
        bool overwrite,
        bool preserveExistingOnFailure,
        ClientIdentity originalIdentity,
        string expectedPatchedSha)
    {
        var destination = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + $".tmp.{Guid.NewGuid():N}";
        string? rollback = null;
        var destinationExisted = File.Exists(destination);

        try
        {
            WriteTemporaryFile(temporary, bytes);
            var temporaryIdentity = PeInspector.Inspect(temporary);
            ValidatePatchedIdentity(originalIdentity, temporaryIdentity, expectedPatchedSha);

            if (destinationExisted)
            {
                if (!overwrite)
                {
                    throw new IOException($"Destination '{destination}' already exists.");
                }

                if (preserveExistingOnFailure)
                {
                    rollback = destination + $".rollback.{Guid.NewGuid():N}";
                    File.Copy(destination, rollback, overwrite: false);
                }
            }

            File.Move(temporary, destination, overwrite);
            var finalIdentity = PeInspector.Inspect(destination);
            ValidatePatchedIdentity(originalIdentity, finalIdentity, expectedPatchedSha);

            if (rollback is not null)
            {
                File.Delete(rollback);
                rollback = null;
            }

            return finalIdentity;
        }
        catch (Exception writeException)
        {
            if (rollback is not null && File.Exists(rollback))
            {
                try
                {
                    File.Move(rollback, destination, overwrite: true);
                    rollback = null;
                }
                catch (Exception rollbackException)
                {
                    throw new IOException(
                        $"Patched output failed and automatic output rollback also failed. " +
                        $"Preserved rollback file: '{rollback}'.",
                        new AggregateException(writeException, rollbackException));
                }
            }
            else if (!destinationExisted && File.Exists(destination))
            {
                File.Delete(destination);
            }

            throw;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
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
            WriteTemporaryFile(temporary, bytes);
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

    private static void WriteTemporaryFile(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
        if (stream.Length != bytes.Length)
        {
            throw new IOException("Temporary output length is invalid.");
        }
    }
}
