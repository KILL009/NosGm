// SPDX-License-Identifier: GPL-3.0-only

namespace NosGM.PacketCatalog;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = new CliOptions(args);
            return options.Command switch
            {
                "generate" => Generate(options),
                "validate" => Validate(options),
                "self-test" => SelfTest.Run(),
                "help" or "--help" or "-h" => Help(),
                _ => throw new ArgumentException($"Unknown command: {options.Command}")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: {exception.Message}");
            return 2;
        }
    }

    private static int Generate(CliOptions options)
    {
        var document = new CatalogAnalyzer(options.Root).Analyze();
        ReportWriter.WriteAll(document, options.OutputDirectory);
        PrintSummary(document, options.OutputDirectory);
        return ExitCode(document, options.Strict);
    }

    private static int Validate(CliOptions options)
    {
        var document = new CatalogAnalyzer(options.Root).Analyze();
        if (options.Report is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(options.Report) ?? Directory.GetCurrentDirectory());
            File.WriteAllText(options.Report, ReportWriter.Serialize(document));
        }
        PrintSummary(document, options.Report);
        return ExitCode(document, options.Strict);
    }

    private static int ExitCode(PacketCatalogDocument document, bool strict)
    {
        if (document.Summary.Errors > 0)
        {
            return 1;
        }
        return strict && document.Summary.Warnings > 0 ? 1 : 0;
    }

    private static void PrintSummary(PacketCatalogDocument document, string? destination)
    {
        Console.WriteLine(
            $"Packets: {document.Summary.PacketTypes}, headers: {document.Summary.PacketHeaders}, " +
            $"typed handlers: {document.Summary.TypedHandlers}, raw handlers: {document.Summary.RawHandlers}.");
        Console.WriteLine(
            $"Diagnostics: {document.Summary.Errors} error(s), {document.Summary.Warnings} warning(s), " +
            $"{document.Summary.Infos} information item(s).");
        if (!string.IsNullOrWhiteSpace(destination))
        {
            Console.WriteLine($"Output: {destination}");
        }
    }

    private static int Help()
    {
        Console.WriteLine("""
NosGM.PacketCatalog 0.1.0

Commands:
  generate  [--root <repo>] [--output-directory <dir>] [--strict]
  validate  [--root <repo>] [--report <catalog.json>] [--strict]
  self-test

The tool parses C# source without loading or executing NosGM server assemblies.
Generated output is deterministic and contains no timestamps.
""");
        return 0;
    }
}
