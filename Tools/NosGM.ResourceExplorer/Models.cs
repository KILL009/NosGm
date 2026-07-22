// SPDX-License-Identifier: BSL-1.0
// Adapted from Pumba98/OnexExplorer at eaee2aa9f0e71b9960da586f425f79e628013021.
// Modifications Copyright (c) 2026 NosGM contributors.

namespace NosGM.ResourceExplorer;

internal enum ArchiveFormat
{
    NosZlib,
    NosText
}

internal sealed record Diagnostic(string Severity, string Code, string Message, int? EntryIndex = null);

internal sealed class ArchiveEntry
{
    public required int Index { get; init; }
    public int? Id { get; init; }
    public required string Name { get; init; }
    public required long Offset { get; init; }
    public required int StoredSize { get; init; }
    public required int UncompressedSize { get; init; }
    public required bool IsCompressed { get; init; }
    public required byte[] Content { get; init; }
    public required string Sha256 { get; init; }
    public string? EncodingHint { get; init; }
}

internal sealed class ArchiveDocument
{
    public required string InputPath { get; init; }
    public required string InputSha256 { get; init; }
    public required long InputSize { get; init; }
    public required ArchiveFormat Format { get; init; }
    public required string Header { get; init; }
    public List<ArchiveEntry> Entries { get; } = [];
    public List<Diagnostic> Diagnostics { get; } = [];
}

internal sealed record CompareEntry(string Key, string Status, string? LeftSha256, string? RightSha256, int? LeftSize, int? RightSize);

internal sealed class CompareReport
{
    public required string LeftPath { get; init; }
    public required string RightPath { get; init; }
    public required string LeftSha256 { get; init; }
    public required string RightSha256 { get; init; }
    public List<CompareEntry> Entries { get; } = [];
}
