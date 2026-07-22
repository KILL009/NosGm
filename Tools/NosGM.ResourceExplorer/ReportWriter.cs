// SPDX-License-Identifier: BSL-1.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.ResourceExplorer;

internal static class ReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeArchive(ArchiveDocument document) => JsonSerializer.Serialize(new
    {
        input_path = document.InputPath,
        input_sha256 = document.InputSha256,
        input_size = document.InputSize,
        format = document.Format.ToString(),
        header = document.Header,
        entry_count = document.Entries.Count,
        diagnostics = document.Diagnostics,
        entries = document.Entries.Select(e => new
        {
            e.Index,
            e.Id,
            e.Name,
            e.Offset,
            e.StoredSize,
            e.UncompressedSize,
            e.IsCompressed,
            e.Sha256,
            e.EncodingHint
        })
    }, Options);

    public static string SerializeCompare(CompareReport report) => JsonSerializer.Serialize(report, Options);

    public static void Write(string? path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine(content);
            return;
        }
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content + Environment.NewLine);
        Console.WriteLine($"Report: {full}");
    }
}
