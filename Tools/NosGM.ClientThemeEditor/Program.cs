// SPDX-License-Identifier: MIT

namespace NosGM.ClientThemeEditor;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            return options.Command switch
            {
                "inspect" => Inspect(options),
                "plan" => Plan(options),
                "apply" => Apply(options),
                "restore" => Restore(options),
                "self-test" => SelfTest.Run(),
                "help" or "--help" or "-h" => Help(),
                _ => throw new ArgumentException($"Unknown command '{options.Command}'.")
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int Inspect(CliOptions options)
    {
        var input = options.Required("input");
        var output = options.Required("profile-output");
        var identity = PeInspector.Inspect(input);
        if (!string.Equals(identity.Architecture, "x86", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Only x86 clients are supported; found {identity.Architecture}.");
        }

        var profile = new ThemeProfile
        {
            ProfileName = $"local-{identity.Sha256[..12].ToLowerInvariant()}",
            ExpectedFileName = identity.FileName,
            ExpectedArchitecture = identity.Architecture,
            ExpectedFileVersion = identity.FileVersion,
            ExpectedLength = identity.Length,
            ExpectedSha256 = identity.Sha256,
            Patches = Array.Empty<PatchDefinition>()
        };

        JsonFiles.Write(output, profile);
        Console.WriteLine($"Profile written: {Path.GetFullPath(output)}");
        Console.WriteLine($"SHA-256: {identity.Sha256}");
        Console.WriteLine("All signature-dependent patches are disabled until reviewed definitions are added.");
        return 0;
    }

    private static int Plan(CliOptions options)
    {
        var input = options.Required("input");
        var profile = JsonFiles.Read<ThemeProfile>(options.Required("profile"));
        var theme = JsonFiles.Read<ThemeDocument>(options.Required("theme"));
        var content = File.ReadAllBytes(input);
        var plan = ThemeEngine.BuildPlan(content, PeInspector.Inspect(input), profile, theme);
        JsonFiles.Write(options.Required("report-output"), plan);
        Console.WriteLine($"{plan.Operations.Count} patch operation(s) validated; no file was modified.");
        return 0;
    }

    private static int Apply(CliOptions options)
    {
        var input = options.Required("input");
        var profile = JsonFiles.Read<ThemeProfile>(options.Required("profile"));
        var theme = JsonFiles.Read<ThemeDocument>(options.Required("theme"));

        PatchManifest manifest;
        if (options.Flag("in-place"))
        {
            if (options.Optional("output") is not null)
            {
                throw new ArgumentException("--output cannot be combined with --in-place.");
            }

            manifest = ThemeEngine.ApplyInPlace(input, profile, theme);
        }
        else
        {
            var output = options.Required("output");
            manifest = ThemeEngine.ApplyToOutput(input, output, profile, theme, options.Flag("force"));
        }

        Console.WriteLine($"Patched SHA-256: {manifest.PatchedSha256}");
        Console.WriteLine($"Operations: {manifest.Operations.Count}");
        if (manifest.BackupPath is not null)
        {
            Console.WriteLine($"Backup: {manifest.BackupPath}");
        }

        return 0;
    }

    private static int Restore(CliOptions options)
    {
        ThemeEngine.Restore(options.Required("manifest"));
        Console.WriteLine("Original client restored and hash verified.");
        return 0;
    }

    private static int Help()
    {
        Console.WriteLine("""
NosGM.ClientThemeEditor

inspect --input <client.exe> --profile-output <profile.json>
plan --input <client.exe> --profile <profile.json> --theme <theme.json> --report-output <plan.json>
apply --input <client.exe> --profile <profile.json> --theme <theme.json> --output <copy.exe> [--force]
apply --input <client.exe> --profile <profile.json> --theme <theme.json> --in-place
restore --manifest <manifest.json>
self-test
""");
        return 0;
    }
}
