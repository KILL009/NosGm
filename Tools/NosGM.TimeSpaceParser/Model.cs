// SPDX-License-Identifier: GPL-3.0-only
// Derived from Elendan/TimeSpace-Generator, the SEOVA adaptation,
// noszanou/OpennosTimeSpaceParser and the OpenNos XML model.
// Modifications Copyright (C) 2026 NosGM contributors.

namespace NosGM.TimeSpaceParser;

internal enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

internal sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    int? LineNumber = null,
    string? RawLine = null);

internal sealed class ParseResult
{
    public TimeSpaceDefinition Definition { get; init; } = new();
    public List<Diagnostic> Diagnostics { get; } = new();
    public int ParsedPacketCount { get; set; }
    public int IgnoredLineCount { get; set; }
}

internal sealed class ValidationResult
{
    public List<Diagnostic> Diagnostics { get; } = new();
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    public bool HasWarnings => Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
}

internal sealed class TimeSpaceDefinition
{
    public TimeSpaceGlobals Globals { get; } = new();
    public List<TimeSpaceRoom> Rooms { get; } = new();
}

internal sealed class TimeSpaceGlobals
{
    public string Name { get; set; } = "Unnamed Time-Space";
    public string Label { get; set; } = "Generated from a packet capture. Review before production use.";
    public byte LevelMinimum { get; set; } = 1;
    public byte LevelMaximum { get; set; } = 99;
    public byte Lives { get; set; } = 1;
    public long Gold { get; set; }
    public int Reputation { get; set; }
    public short StartX { get; set; }
    public short StartY { get; set; }
    public List<RewardItem> DrawItems { get; } = new();
    public List<RewardItem> SpecialItems { get; } = new();
    public List<RewardItem> GiftItems { get; } = new();
    public List<RewardItem> RequiredItems { get; } = new();
}

internal sealed record RewardItem(short VNum, short Amount);

internal sealed class TimeSpaceRoom
{
    public int Id { get; init; }
    public int RuntimeMapId { get; init; }
    public short VNum { get; init; }
    public short EntryX { get; init; }
    public short EntryY { get; init; }
    public byte IndexX { get; set; }
    public byte IndexY { get; set; }
    public bool DropAllowed { get; set; }
    public short XpRate { get; set; }
    public int? GenerateClock { get; set; }
    public int? GenerateMapClock { get; set; }
    public bool StartClock { get; set; }
    public bool StartMapClock { get; set; }
    public EventBucket Discovery { get; } = new();
    public EventBucket Move { get; } = new();
    public EventBucket Clean { get; } = new();
    public EventBucket FirstEnable { get; } = new();
    public List<PortalDefinition> Portals { get; } = new();
    public List<ButtonDefinition> Buttons { get; } = new();
    public HashSet<int> SeenButtonEntityIds { get; } = new();
    public HashSet<string> SeenPortalKeys { get; } = new(StringComparer.Ordinal);
}

internal sealed class EventBucket
{
    public List<MonsterDefinition> Monsters { get; } = new();
    public List<NpcDefinition> Npcs { get; } = new();
    public List<MessageDefinition> Messages { get; } = new();
    public List<string> Packets { get; } = new();
    public List<int> Dialogs { get; } = new();
    public List<PortalTypeChange> PortalTypeChanges { get; } = new();
    public List<int> GeneratedClocks { get; } = new();
    public bool RefreshMapItems { get; set; }
    public bool OnMapClean { get; set; }

    public bool HasContent =>
        Monsters.Count > 0 ||
        Npcs.Count > 0 ||
        Messages.Count > 0 ||
        Packets.Count > 0 ||
        Dialogs.Count > 0 ||
        PortalTypeChanges.Count > 0 ||
        GeneratedClocks.Count > 0 ||
        RefreshMapItems ||
        OnMapClean;
}

internal sealed class MonsterDefinition
{
    public short VNum { get; init; }
    public int EntityId { get; init; }
    public short PositionX { get; init; }
    public short PositionY { get; init; }
    public bool Move { get; set; } = true;
    public bool IsBonus { get; set; }
    public bool IsHostile { get; set; }
    public bool IsTarget { get; set; }
    public bool IsDead { get; set; }
    public EventBucket OnDeath { get; } = new();
}

internal sealed class NpcDefinition
{
    public short VNum { get; init; }
    public int EntityId { get; init; }
    public short PositionX { get; init; }
    public short PositionY { get; init; }
    public byte Direction { get; init; }
    public bool Move { get; set; } = true;
    public bool IsProtected { get; set; }
    public bool IsHostile { get; set; }
}

internal sealed record MessageDefinition(byte Type, string Value);
internal sealed record PortalTypeChange(int PortalId, sbyte Type);

internal sealed class PortalDefinition
{
    public byte IdOnMap { get; init; }
    public short PositionX { get; init; }
    public short PositionY { get; init; }
    public short Type { get; init; }
    public int DestinationRuntimeMapId { get; init; }
    public short ToMap { get; set; }
    public short ToX { get; set; }
    public short ToY { get; set; }
    public bool EndsInstance { get; set; }
    public bool DestinationWasInferred { get; set; }
}

internal sealed class ButtonDefinition
{
    public int Id { get; init; }
    public short PositionX { get; init; }
    public short PositionY { get; init; }
    public short VNumEnabled { get; init; }
    public short VNumDisabled { get; init; }
}

internal enum CapturePhase
{
    Initial,
    Active,
    FirstEnable
}
