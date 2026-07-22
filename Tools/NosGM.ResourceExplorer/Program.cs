// SPDX-License-Identifier: BSL-1.0
// Adapted from archive concepts in Pumba98/OnexExplorer.

namespace NosGM.ResourceExplorer;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var cli = new Cli(args);
            return cli.Command switch
            {
                "inspect" or "list" => Inspect(cli),
                "extract" => Extract(cli),
                "compare" => Compare(cli),
                "self-test" => RunSelfTest(),
                "help" or "--help" or "-h" => Help(),
                _ => throw new ArgumentException($"Unknown command: {cli.Command}")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"ERROR: {exception.Message}");
            return 2;
        }
    }

    private static int Inspect(Cli cli)
    {
        var document = ArchiveReader.Read(cli.Required("--input"));
        ReportWriter.Write(cli.Optional("--report"), ReportWriter.SerializeArchive(document));
        return 0;
    }

    private static int Extract(Cli cli)
    {
        var document = ArchiveReader.Read(cli.Required("--input"));
        var output = Path.GetFullPath(cli.Required("--output-directory"));
        var force = cli.Flag("--force");
        long total = 0;
        foreach (var entry in document.Entries)
        {
            total = checked(total + entry.Content.LongLength);
            if (total > 2L * 1024 * 1024 * 1024)
            {
                throw new InvalidDataException("Extraction exceeds the 2 GiB per-run safety limit.");
            }
            var path = ExtractionSandbox.GetSafePath(output, entry);
            if (File.Exists(path) && !force)
            {
                throw new IOException($"Refusing to overwrite {path}. Use --force to replace extracted output only.");
            }
            File.WriteAllBytes(path, entry.Content);
        }
        ReportWriter.Write(cli.Optional("--report") ?? Path.Combine(output, "nosgm-resource-report.json"), ReportWriter.SerializeArchive(document));
        Console.WriteLine($"Extracted {document.Entries.Count} entries into {output}.");
        return 0;
    }

    private static int Compare(Cli cli)
    {
        var left = ArchiveReader.Read(cli.Required("--left"));
        var right = ArchiveReader.Read(cli.Required("--right"));
        var report = new CompareReport
        {
            LeftPath = left.InputPath,
            RightPath = right.InputPath,
            LeftSha256 = left.InputSha256,
            RightSha256 = right.InputSha256
        };
        var leftEntries = left.Entries.ToDictionary(Key, StringComparer.OrdinalIgnoreCase);
        var rightEntries = right.Entries.ToDictionary(Key, StringComparer.OrdinalIgnoreCase);
        foreach (var key in leftEntries.Keys.Union(rightEntries.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            leftEntries.TryGetValue(key, out var a);
            rightEntries.TryGetValue(key, out var b);
            var status = a is null ? "added" : b is null ? "removed" : a.Sha256 == b.Sha256 ? "unchanged" : "changed";
            report.Entries.Add(new CompareEntry(key, status, a?.Sha256, b?.Sha256, a?.UncompressedSize, b?.UncompressedSize));
        }
        ReportWriter.Write(cli.Optional("--report"), ReportWriter.SerializeCompare(report));
        return report.Entries.Any(entry => entry.Status != "unchanged") ? 1 : 0;
    }

    private static string Key(ArchiveEntry entry) => $"{entry.Id?.ToString() ?? "none"}:{entry.Name}:{entry.Index}";

    private static int RunSelfTest()
    {
        SelfTest.Run();
        return 0;
    }

    private static int Help()
    {
        Console.WriteLine("""
NosGM.ResourceExplorer 0.1.0

Commands:
  inspect --input <archive.NOS> [--report <report.json>]
  list    --input <archive.NOS> [--report <report.json>]
  extract --input <archive.NOS> --output-directory <dir> [--report <report.json>] [--force]
  compare --left <archive.NOS> --right <archive.NOS> [--report <report.json>]
  self-test

This release is read-only with respect to source archives. It does not repack or patch .NOS files.
""");
        return 0;
    }
}
