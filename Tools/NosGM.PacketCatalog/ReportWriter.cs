// SPDX-License-Identifier: GPL-3.0-only

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NosGM.PacketCatalog;

internal static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void WriteAll(PacketCatalogDocument document, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "packet-catalog.json"), Serialize(document), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDirectory, "diagnostics.json"), Serialize(document.Diagnostics), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDirectory, "PACKETS.md"), Markdown(document), new UTF8Encoding(false));
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine;

    private static string Markdown(PacketCatalogDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# NosGM packet catalog");
        builder.AppendLine();
        builder.AppendLine("Generated deterministically from C# source by `Tools/NosGM.PacketCatalog`.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| C# source files | {document.Summary.SourceFiles} |");
        builder.AppendLine($"| Packet types | {document.Summary.PacketTypes} |");
        builder.AppendLine($"| Packet headers | {document.Summary.PacketHeaders} |");
        builder.AppendLine($"| Typed handlers | {document.Summary.TypedHandlers} |");
        builder.AppendLine($"| Raw handlers | {document.Summary.RawHandlers} |");
        builder.AppendLine($"| Errors | {document.Summary.Errors} |");
        builder.AppendLine($"| Warnings | {document.Summary.Warnings} |");
        builder.AppendLine($"| Information | {document.Summary.Infos} |");
        builder.AppendLine();

        builder.AppendLine("## Packet index");
        builder.AppendLine();
        builder.AppendLine("| Header | Type | Direction | Fields | Handlers | Source |");
        builder.AppendLine("|---|---|---|---:|---:|---|");
        foreach (var packet in document.Packets.Where(packet => !packet.IsSubPacket)
                     .OrderBy(packet => packet.Headers.FirstOrDefault() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(packet => packet.FullName, StringComparer.Ordinal))
        {
            builder.AppendLine($"| {Escape(string.Join(", ", packet.Headers.Select(Code)))} | {Code(packet.FullName)} | {Escape(packet.Direction)} | {packet.Properties.Count} | {packet.Handlers.Count} | {Code($"{packet.Source.Path}:{packet.Source.Line}")} |");
        }
        builder.AppendLine();

        foreach (var packet in document.Packets.OrderBy(packet => packet.FullName, StringComparer.Ordinal))
        {
            builder.AppendLine($"## {packet.FullName}");
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(packet.Summary))
            {
                builder.AppendLine(packet.Summary);
                builder.AppendLine();
            }

            builder.AppendLine($"- Headers: {(packet.Headers.Count == 0 ? "_subpacket / none_" : string.Join(", ", packet.Headers.Select(Code)))}");
            builder.AppendLine($"- Direction: `{packet.Direction}`");
            builder.AppendLine($"- Evidence: {packet.DirectionEvidence}");
            builder.AppendLine($"- Authority: `{packet.Authority}`");
            builder.AppendLine($"- Character screen: `{packet.IsCharScreen.ToString().ToLowerInvariant()}`");
            builder.AppendLine($"- Pass non-parseable: `{packet.PassNonParseablePacket.ToString().ToLowerInvariant()}`");
            builder.AppendLine($"- Amount: `{packet.Amount}`");
            builder.AppendLine($"- Source: {Code($"{packet.Source.Path}:{packet.Source.Line}")}");
            builder.AppendLine();

            if (packet.Properties.Count > 0)
            {
                builder.AppendLine("### Fields");
                builder.AppendLine();
                builder.AppendLine("| Index | Name | Type | Return | To end | Remove separator | Source |");
                builder.AppendLine("|---:|---|---|---|---|---|---|");
                foreach (var property in packet.Properties)
                {
                    builder.AppendLine($"| {property.Index} | {Code(property.Name)} | {Code(property.Type)} | {Bool(property.IsReturnPacket)} | {Bool(property.SerializeToEnd)} | {Bool(property.RemoveSeparator)} | {Code($"{property.Source.Path}:{property.Source.Line}")} |");
                }
                builder.AppendLine();
            }

            if (packet.Handlers.Count > 0)
            {
                builder.AppendLine("### Handlers");
                builder.AppendLine();
                foreach (var handler in packet.Handlers)
                {
                    builder.AppendLine($"- {Code($"{handler.ContainingType}.{handler.Method}")} at {Code($"{handler.Source.Path}:{handler.Source.Line}")}");
                }
                builder.AppendLine();
            }
        }

        if (document.RawHandlers.Count > 0)
        {
            builder.AppendLine("## Raw header handlers");
            builder.AppendLine();
            builder.AppendLine("| Headers | Handler | Amount | Source |");
            builder.AppendLine("|---|---|---:|---|");
            foreach (var handler in document.RawHandlers)
            {
                builder.AppendLine($"| {Escape(string.Join(", ", handler.Headers.Select(Code)))} | {Code($"{handler.ContainingType}.{handler.Method}")} | {handler.Amount} | {Code($"{handler.Source.Path}:{handler.Source.Line}")} |");
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Diagnostics");
        builder.AppendLine();
        if (document.Diagnostics.Count == 0)
        {
            builder.AppendLine("No diagnostics.");
        }
        else
        {
            builder.AppendLine("| Severity | Code | Packet | Message | Source |");
            builder.AppendLine("|---|---|---|---|---|");
            foreach (var diagnostic in document.Diagnostics)
            {
                builder.AppendLine($"| {diagnostic.Severity} | {Code(diagnostic.Code)} | {Escape(diagnostic.Packet ?? string.Empty)} | {Escape(diagnostic.Message)} | {Code($"{diagnostic.Source.Path}:{diagnostic.Source.Line}")} |");
            }
        }

        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string Bool(bool value) => value ? "yes" : "no";
    private static string Code(string value) => $"`{value.Replace("`", "\\`", StringComparison.Ordinal)}`";
    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
