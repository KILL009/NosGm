// SPDX-License-Identifier: MIT

namespace NosGM.ClientThemeEditor;

internal static class SelfTest
{
    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "NosGM.ClientThemeEditor.SelfTest", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var client = Path.Combine(root, "NostaleClientX.exe");
            var originalBytes = BuildSyntheticPe();
            File.WriteAllBytes(client, originalBytes);

            var identity = PeInspector.Inspect(client);
            Require(identity.Architecture == "x86", "Synthetic PE architecture was not recognized.");

            var profile = new ThemeProfile
            {
                ProfileName = "synthetic-x86",
                ExpectedFileName = identity.FileName,
                ExpectedArchitecture = identity.Architecture,
                ExpectedFileVersion = identity.FileVersion,
                ExpectedLength = identity.Length,
                ExpectedSha256 = identity.Sha256,
                Patches =
                [
                    new PatchDefinition
                    {
                        Id = "gm-tag",
                        Description = "Synthetic GM tag color",
                        Enabled = true,
                        PatternHex = "DE AD ?? EF 10 20 30 40",
                        ExpectedMatches = 1,
                        ValueOffset = 4,
                        ExpectedOriginalHex = "10 20 30 40",
                        ColorEncoding = "RGBA"
                    }
                ]
            };

            var theme = new ThemeDocument
            {
                ThemeName = "synthetic",
                Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["gm-tag"] = "#11223344"
                }
            };

            var originalHash = identity.Sha256;
            var output = Path.Combine(root, "patched.exe");
            var manifest = ThemeEngine.ApplyToOutput(client, output, profile, theme, overwrite: false);
            Require(PeInspector.Inspect(client).Sha256 == originalHash, "Copy mode modified the original file.");
            var patched = File.ReadAllBytes(output);
            Require(patched.AsSpan(0xC4, 4).SequenceEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }),
                "Replacement bytes were not written at the expected offset.");
            Require(manifest.Operations.Count == 1, "Expected one patch operation.");
            Require(File.Exists(output + ".nosgm-theme-manifest.json"), "Copy-mode manifest was not written.");

            var changedContent = originalBytes.ToArray();
            changedContent[0x100] = 0x7F;
            ExpectInvalidData(() => ThemeEngine.BuildPlan(changedContent, identity, profile, theme),
                "Content hash mismatch was accepted.");

            var unknownTheme = theme with
            {
                Colors = new Dictionary<string, string>
                {
                    ["unknown-color"] = "#11223344"
                }
            };
            ExpectInvalidData(() => ThemeEngine.BuildPlan(originalBytes, identity, profile, unknownTheme),
                "Unknown theme color was accepted.");

            ExpectInvalidData(() => ThemeEngine.BuildPlan(
                originalBytes,
                identity,
                profile with { ResearchOnly = true },
                theme), "Research-only profile was accepted.");

            var duplicate = BuildSyntheticPe();
            Array.Copy(duplicate, 0xC0, duplicate, 0xD0, 8);
            var duplicateClient = Path.Combine(root, "duplicate.exe");
            File.WriteAllBytes(duplicateClient, duplicate);
            var duplicateIdentity = PeInspector.Inspect(duplicateClient);
            var duplicateProfile = profile with
            {
                ExpectedFileName = duplicateIdentity.FileName,
                ExpectedLength = duplicateIdentity.Length,
                ExpectedSha256 = duplicateIdentity.Sha256
            };
            ExpectInvalidData(() => ThemeEngine.BuildPlan(
                duplicate,
                duplicateIdentity,
                duplicateProfile,
                theme), "Duplicate signature was accepted.");

            var inplace = Path.Combine(root, "inplace.exe");
            File.Copy(client, inplace);
            var inplaceIdentity = PeInspector.Inspect(inplace);
            var inplaceProfile = profile with
            {
                ExpectedFileName = inplaceIdentity.FileName,
                ExpectedLength = inplaceIdentity.Length,
                ExpectedSha256 = inplaceIdentity.Sha256
            };
            var inplaceManifest = ThemeEngine.ApplyInPlace(inplace, inplaceProfile, theme);
            var manifestPath = Directory.GetFiles(Path.Combine(root, "NosGM.ThemeBackups"), "manifest.json",
                SearchOption.AllDirectories).Single();
            Require(PeInspector.Inspect(inplace).Sha256 == inplaceManifest.PatchedSha256,
                "In-place patched hash differs from the manifest.");

            var expectedPatchedBytes = File.ReadAllBytes(inplace);
            var externallyChanged = expectedPatchedBytes.ToArray();
            externallyChanged[0x100] ^= 0xFF;
            File.WriteAllBytes(inplace, externallyChanged);
            ExpectInvalidData(() => ThemeEngine.Restore(manifestPath),
                "Restore accepted a client changed after patching.");

            File.WriteAllBytes(inplace, expectedPatchedBytes);
            ThemeEngine.Restore(manifestPath);
            Require(PeInspector.Inspect(inplace).Sha256 == inplaceIdentity.Sha256,
                "Restore did not recover the original hash.");

            Console.WriteLine("NosGM.ClientThemeEditor synthetic self-test passed.");
            return 0;
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static byte[] BuildSyntheticPe()
    {
        var bytes = new byte[512];
        bytes[0] = 0x4D;
        bytes[1] = 0x5A;
        BitConverter.GetBytes(0x80).CopyTo(bytes, 0x3C);
        bytes[0x80] = 0x50;
        bytes[0x81] = 0x45;
        bytes[0x82] = 0;
        bytes[0x83] = 0;
        bytes[0x84] = 0x4C;
        bytes[0x85] = 0x01;
        new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x10, 0x20, 0x30, 0x40 }.CopyTo(bytes, 0xC0);
        return bytes;
    }

    private static void ExpectInvalidData(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
