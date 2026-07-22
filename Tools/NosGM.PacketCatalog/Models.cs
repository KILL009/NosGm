// SPDX-License-Identifier: GPL-3.0-only
// Inspired by packet-documentation concepts in BlowaXD/SaltyEmu.

namespace NosGM.PacketCatalog;

internal enum CatalogSeverity
{
    Info,
    Warning,
    Error
}

internal sealed record SourceReference(string Path, int Line);

internal sealed record PacketPropertyDescriptor(
    string Name,
    string Type,
    int Index,
    bool IsReturnPacket,
    bool SerializeToEnd,
    bool RemoveSeparator,
    string? Summary,
    SourceReference Source);

internal sealed record HandlerDescriptor(
    string Kind,
    string ContainingType,
    string Method,
    IReadOnlyList<string> Headers,
    int Amount,
    string? PacketType,
    SourceReference Source);

internal sealed class PacketDescriptor
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string Namespace { get; init; }
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<PacketPropertyDescriptor> Properties { get; init; }
    public required IReadOnlyList<HandlerDescriptor> Handlers { get; set; }
    public required SourceReference Source { get; init; }
    public string? Summary { get; init; }
    public string Direction { get; set; } = "Unknown";
    public string DirectionEvidence { get; set; } = "No handler or reliable source-path evidence was found.";
    public bool IsSubPacket { get; init; }
    public bool IsCharScreen { get; init; }
    public bool PassNonParseablePacket { get; init; }
    public int Amount { get; init; } = 1;
    public string Authority { get; init; } = "User";
    public IReadOnlyList<string> Authorities { get; init; } = Array.Empty<string>();
}

internal sealed record CatalogDiagnostic(
    string Code,
    CatalogSeverity Severity,
    string Message,
    SourceReference Source,
    string? Packet = null,
    string? Header = null);

internal sealed class CatalogSummary
{
    public int SourceFiles { get; init; }
    public int PacketTypes { get; init; }
    public int PacketHeaders { get; init; }
    public int TypedHandlers { get; init; }
    public int RawHandlers { get; init; }
    public int Errors { get; init; }
    public int Warnings { get; init; }
    public int Infos { get; init; }
}

internal sealed class PacketCatalogDocument
{
    public required string Root { get; init; }
    public required CatalogSummary Summary { get; init; }
    public required IReadOnlyList<PacketDescriptor> Packets { get; init; }
    public required IReadOnlyList<HandlerDescriptor> RawHandlers { get; init; }
    public required IReadOnlyList<CatalogDiagnostic> Diagnostics { get; init; }
}
