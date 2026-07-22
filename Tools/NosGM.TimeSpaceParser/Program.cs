// SPDX-License-Identifier: GPL-3.0-only
// Derived from Elendan/TimeSpace-Generator, the SEOVA adaptation,
// noszanou/OpennosTimeSpaceParser and the OpenNos XML model.
// Modifications Copyright (C) 2026 NosGM contributors.

using System.Text;

namespace NosGM.TimeSpaceParser;

internal static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            var options = CliOptions.Parse(args);
            if (options.Help)
            {
                PrintHelp();
                return 0;
            }

            return options.Command switch
            {
                "parse" => RunParse(options),
                "batch" => RunBatch(options),
                "validate" => RunValidate(options),
                "self-test" => RunSelfTest(),
                _ => Fail($"Unknown command: {options.Command}")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: {exception.Message}");
            return 1;
        }
    }

    private static int RunParse(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            throw new ArgumentException("parse requires --input <capture.txt>.");
        }
        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            throw new ArgumentException("parse requires --output <timespace.xml>.");
        }

        return ParseOne(options.InputPath, options.OutputPath, options);
    }

    private static int RunBatch(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InputDirectory))
        {
            throw new ArgumentException("batch requires --input-directory <directory>.");
        }
        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            throw new ArgumentException("batch requires --output-directory <directory>.");
        }
        if (!Directory.Exists(options.InputDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {options.InputDirectory}");
        }

        Directory.CreateDirectory(options.OutputDirectory);
        var files = Directory.GetFiles(options.InputDirectory, options.Pattern, SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No files matched {options.Pattern} in {options.InputDirectory}.");
            return 1;
        }

        var failed = 0;
        foreach (var input in files)
        {
            var output = Path.Combine(options.OutputDirectory, Path.GetFileNameWithoutExtension(input) + ".xml");
            Console.WriteLine($"Parsing {Path.GetFileName(input)}...");
            if (ParseOne(input, output, options) != 0)
            {
                failed++;
            }
        }

        Console.WriteLine($"Batch complete: {files.Count - failed} succeeded, {failed} failed.");
        return failed == 0 ? 0 : 2;
    }

    private static int RunValidate(CliOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            throw new ArgumentException("validate requires --input <timespace.xml>.");
        }
        if (!File.Exists(options.InputPath))
        {
            throw new FileNotFoundException("XML file not found.", options.InputPath);
        }

        var validation = TimeSpaceValidator.ValidateXml(options.InputPath);
        ReportWriter.WriteValidationReport(options.InputPath, validation);
        PrintDiagnostics(validation.Diagnostics);
        if (validation.HasErrors || (options.Strict && validation.HasWarnings))
        {
            return 2;
        }

        Console.WriteLine("XML validation passed.");
        return 0;
    }

    private static int ParseOne(string inputPath, string outputPath, CliOptions options)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Capture file not found.", inputPath);
        }

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var parser = new CaptureParser();
        var parseResult = parser.Parse(File.ReadLines(inputPath), options, inputPath);
        var validation = TimeSpaceValidator.Validate(parseResult.Definition);
        ReportWriter.WriteParseReports(outputPath, inputPath, parseResult, validation);

        var allDiagnostics = parseResult.Diagnostics.Concat(validation.Diagnostics).ToList();
        PrintDiagnostics(allDiagnostics);
        var strictFailure = options.Strict && allDiagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        if (validation.HasErrors || strictFailure)
        {
            Console.Error.WriteLine("XML was not written because validation failed. Review the generated reports.");
            return 2;
        }

        TimeSpaceXml.Save(parseResult.Definition, outputPath, options.Force);
        var xmlValidation = TimeSpaceValidator.ValidateXml(outputPath);
        if (xmlValidation.HasErrors)
        {
            PrintDiagnostics(xmlValidation.Diagnostics);
            Console.Error.WriteLine("Generated XML failed the structural validation pass.");
            return 3;
        }

        Console.WriteLine($"Generated: {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Rooms: {parseResult.Definition.Rooms.Count}; packets: {parseResult.ParsedPacketCount}; ignored lines: {parseResult.IgnoredLineCount}");
        return 0;
    }

    private static int RunSelfTest()
    {
        const string capture = """
            rbr 1 0 0 1.99 0 -1.0 -1.0 -1.0 -1.0 -1.0 -1.0 -1.0 -1.0 -1.0 -1.0 0. 0 0 Training Time-Space
            Synthetic validation capture
            at 100 1 10 10 0 0 0 0
            rsfn 0 0
            in 3 TestMonster 100 5000 12 12 0 100 0 0
            msg 0 Welcome_to_the_first_room
            gp 15 1 101 1 0 0
            mapclean
            at 101 2 15 28 0 0 0 0
            rsfn 1 0
            in 2 TestNpc 200 6000 14 20 2 100 0 0
            gp 15 28 100 1 0 0
            gp 20 20 -1 5 1 0
            """;

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "NosGM.TimeSpaceParser.SelfTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var capturePath = Path.Combine(temporaryDirectory, "packet.txt");
            var xmlPath = Path.Combine(temporaryDirectory, "timespace.xml");
            File.WriteAllText(capturePath, capture, new UTF8Encoding(false));
            var options = CliOptions.Parse(new[] { "parse", "--input", capturePath, "--output", xmlPath, "--strict" });
            var exitCode = ParseOne(capturePath, xmlPath, options);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Self-test parse failed with exit code {exitCode}.");
                return exitCode;
            }

            var validation = TimeSpaceValidator.ValidateXml(xmlPath);
            if (validation.HasErrors)
            {
                PrintDiagnostics(validation.Diagnostics);
                return 3;
            }

            var xml = File.ReadAllText(xmlPath);
            if (!xml.Contains("<CreateMap", StringComparison.Ordinal) || !xml.Contains("<OnTraversal>", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Self-test XML is missing expected map or end-event elements.");
                return 4;
            }

            Console.WriteLine("NosGM.TimeSpaceParser self-test passed.");
            return 0;
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static void PrintDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var line = diagnostic.LineNumber.HasValue ? $" line {diagnostic.LineNumber.Value}" : string.Empty;
            Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Code}{line}: {diagnostic.Message}");
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            NosGM.TimeSpaceParser

            Commands:
              parse     Convert one packet capture to NosGM/OpenNos Time-Space XML.
              batch     Convert all matching captures in a directory.
              validate  Validate an existing Time-Space XML file.
              self-test Run the package-free synthetic regression test.

            Parse:
              dotnet run --project NosGM.TimeSpaceParser.csproj -- parse \
                --input packet.txt --output timespace.xml [--strict] [--force] \
                [--name "Time-Space name"] [--label "Description"] \
                [--lives 1] [--gold 0] [--reputation 0]

            Batch:
              dotnet run --project NosGM.TimeSpaceParser.csproj -- batch \
                --input-directory Captures --output-directory Output [--pattern "*.txt"]

            Validate:
              dotnet run --project NosGM.TimeSpaceParser.csproj -- validate \
                --input timespace.xml [--strict]
            """);
    }
}
