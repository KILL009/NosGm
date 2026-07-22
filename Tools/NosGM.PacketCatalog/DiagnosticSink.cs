// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NosGM.PacketCatalog;

internal sealed class DiagnosticSink
{
    private readonly List<CatalogDiagnostic> _items = new();

    public IReadOnlyList<CatalogDiagnostic> Items => _items;

    public void Clear() => _items.Clear();

    public void Add(
        string code,
        CatalogSeverity severity,
        string message,
        string path,
        int line,
        string? packet = null,
        string? header = null) =>
        _items.Add(new CatalogDiagnostic(code, severity, message, new SourceReference(path, line), packet, header));
}

internal sealed record SourceSyntaxFile(string Path, CompilationUnitSyntax Root);
