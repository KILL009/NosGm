// SPDX-License-Identifier: GPL-3.0-only
// Inspired by packet-documentation concepts in BlowaXD/SaltyEmu.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NosGM.PacketCatalog;

internal sealed class CatalogAnalyzer
{
    private readonly string _root;
    private readonly DiagnosticSink _diagnostics = new();

    public CatalogAnalyzer(string root)
    {
        _root = Path.GetFullPath(root);
    }

    public PacketCatalogDocument Analyze()
    {
        _diagnostics.Clear();
        var files = SourceDiscovery.FindCSharpFiles(_root);
        var syntaxFiles = files.Select(ParseFile).ToArray();
        var packetReader = new PacketSourceReader(_diagnostics);
        var handlerReader = new HandlerSourceReader(_diagnostics);

        var packets = syntaxFiles
            .SelectMany(packetReader.Read)
            .OrderBy(packet => packet.FullName, StringComparer.Ordinal)
            .ToList();
        var rawHandlers = new List<HandlerDescriptor>();
        var typedCandidates = new List<TypedHandlerCandidate>();

        foreach (var syntaxFile in syntaxFiles)
        {
            handlerReader.Read(syntaxFile, rawHandlers, typedCandidates);
        }

        handlerReader.LinkTypedHandlers(packets, typedCandidates);
        new CatalogValidator(_diagnostics).Validate(packets, rawHandlers);

        var diagnostics = _diagnostics.Items
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Source.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Source.Line)
            .ToArray();
        var orderedRawHandlers = rawHandlers
            .OrderBy(handler => handler.Headers.FirstOrDefault() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(handler => handler.ContainingType, StringComparer.Ordinal)
            .ThenBy(handler => handler.Method, StringComparer.Ordinal)
            .ToArray();

        return new PacketCatalogDocument
        {
            Root = ".",
            Summary = new CatalogSummary
            {
                SourceFiles = syntaxFiles.Length,
                PacketTypes = packets.Count,
                PacketHeaders = packets.SelectMany(packet => packet.Headers)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                TypedHandlers = packets.Sum(packet => packet.Handlers.Count),
                RawHandlers = orderedRawHandlers.Length,
                Errors = diagnostics.Count(item => item.Severity == CatalogSeverity.Error),
                Warnings = diagnostics.Count(item => item.Severity == CatalogSeverity.Warning),
                Infos = diagnostics.Count(item => item.Severity == CatalogSeverity.Info)
            },
            Packets = packets,
            RawHandlers = orderedRawHandlers,
            Diagnostics = diagnostics
        };
    }

    private SourceSyntaxFile ParseFile(string path)
    {
        var relative = SourceDiscovery.NormalizeRelative(_root, path);
        var text = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(
            text,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            path: relative);

        foreach (var diagnostic in tree.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error))
        {
            var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
            _diagnostics.Add("SRC001", CatalogSeverity.Error, diagnostic.GetMessage(), relative, line);
        }

        return new SourceSyntaxFile(relative, tree.GetCompilationUnitRoot());
    }
}
