// SPDX-License-Identifier: MIT

using System.Text.Json;

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
                "self-test" => SelfTestCommand(options),
                "help" or "--help" or "-h" => HelpCommand(options),
                _ => throw new ArgumentException($"Unknown command '{options.Command}'.")
            };
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or InvalidDataException or
            InvalidOperationException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int Inspect(CliOptions options)
    {
        options.EnsureOnly("input", "profile-output", "force");
        var input = options.Required("input");
        var output = Path.GetFullPath(options.Required("profile-output"));
        var force = options.Flag("force");
        if (File.Exists(output) && !force)
        {
            throw new IOException($"Profile '{output}' already exists. Use --force to replace it.");
        }

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
        Console.WriteLine($"Profile written: {output}");
        Console.WriteLine($"SHA-256: {identity.Sha256}");
        Console.WriteLine("All signature-dependent patches are disabled until reviewed definitions are added.");
        return 0;
    }

    private static int Plan(CliOptions options)
    {
        options.EnsureOnly("input", "profile", "theme", "report-output", "force");
        var input = options.Required("input");
        var reportOutput = Path.GetFullPath(options.Required("report-output"));
        var force = options.Flag("force");
        if (File.Exists(reportOutput) && !force)
        {
            throw new IOException($"Plan '{reportOutput}' already exists. Use --force to replace it.");
        }

        var profile = JsonFiles.Read<ThemeProfile>(options.Required("profile"));
        var theme = JsonFiles.Read<ThemeDocument>(options.Required("theme"));
        var content = File.ReadAllBytes(input);
        var plan = ThemeEngine.BuildPlan(content, PeInspector.Inspect(input), profile, theme);
        JsonFiles.Write(reportOutput, plan);
        Console.WriteLine($"{plan.Operations.Count} patch operation(s) validated; no file was modified.");
        return 0;
    }

    private static int Apply(CliOptions options)
    {
        options.EnsureOnly("input", "profile", "theme", "output", "in-place", "force");
        var input = options.Required("input");
        var profile = JsonFiles.Read<ThemeProfile>(options.Required("profile"));
        var theme = JsonFiles.Read<ThemeDocument>(options.Required("theme"));
        var inPlace = options.Flag("in-place");
        var force = options.Flag("force");

        PatchManifest manifest;
        if (inPlace)
        {
            if (options.Optional("output") is not null)
            {
                throw new ArgumentException("--output cannot be combined with --in-place.");
            }

            if (force)
            {
                throw new ArgumentException("--force is not supported with guarded --in-place mode.");
            }

            manifest = ThemeEngine.ApplyInPlace(input, profile, theme);
        }
        else
        {
            var output = options.Required("output");
            manifest = ThemeEngine.ApplyToOutput(input, output, profile, theme, force);
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
        options.EnsureOnly("manifest");
        ThemeEngine.Restore(options.Required("manifest"));
        Console.WriteLine("Original client restored and hash verified.");
        return 0;
    }

    private static int SelfTestCommand(CliOptions options)
    {
        options.EnsureOnly();
        return SelfTest.Run();
    }

    private static int HelpCommand(CliOptions options)
    {
        options.EnsureOnly();
        return Help();
    }

    private static int Help()
    {
        Console.WriteLine("""
NosGM.ClientThemeEditor

inspect --input <client.exe> --profile-output <profile.json> [--force]
plan --input <client.exe> --profile <profile.json> --theme <theme.json> --report-output <plan.json> [--force]
apply --input <client.exe> --profile <profile.json> --theme <theme.json> --output <copy.exe> [--force]
apply --input <client.exe> --profile <profile.json> --theme <theme.json> --in-place
restore --manifest <manifest.json>
self-test
""");
        return 0;
    }
}
