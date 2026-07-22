// SPDX-License-Identifier: GPL-3.0-only
// Derived from Elendan/TimeSpace-Generator, the SEOVA adaptation,
// noszanou/OpennosTimeSpaceParser and the OpenNos XML model.
// Modifications Copyright (C) 2026 NosGM contributors.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace NosGM.TimeSpaceParser;

internal static class TimeSpaceXml
{
    public static string Serialize(TimeSpaceDefinition definition)
    {
        var globals = new XElement("Globals");
        AddItems(globals, "DrawItems", definition.Globals.DrawItems);
        AddItems(globals, "GiftItems", definition.Globals.GiftItems);
        globals.Add(ValueElement("Gold", definition.Globals.Gold));
        globals.Add(ValueElement("Label", definition.Globals.Label));
        globals.Add(ValueElement("LevelMaximum", definition.Globals.LevelMaximum));
        globals.Add(ValueElement("LevelMinimum", definition.Globals.LevelMinimum));
        globals.Add(ValueElement("Lives", definition.Globals.Lives));
        globals.Add(ValueElement("Name", definition.Globals.Name));
        globals.Add(ValueElement("Reputation", definition.Globals.Reputation));
        AddItems(globals, "RequiredItems", definition.Globals.RequiredItems);
        AddItems(globals, "SpecialItems", definition.Globals.SpecialItems);
        globals.Add(ValueElement("StartX", definition.Globals.StartX));
        globals.Add(ValueElement("StartY", definition.Globals.StartY));

        var instanceEvents = new XElement("InstanceEvents");
        foreach (var room in definition.Rooms.OrderBy(static room => room.Id))
        {
            instanceEvents.Add(CreateRoomElement(room));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Definition", globals, instanceEvents));

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = false
        };

        using var writer = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        {
            document.Save(xmlWriter);
        }

        return writer.ToString();
    }

    public static void Save(TimeSpaceDefinition definition, string path, bool force)
    {
        if (File.Exists(path) && !force)
        {
            throw new IOException($"Output file already exists: {path}. Use --force to replace it.");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(definition), new UTF8Encoding(false));
    }

    private static XElement CreateRoomElement(TimeSpaceRoom room)
    {
        var element = new XElement(
            "CreateMap",
            new XAttribute("DropAllowed", room.DropAllowed),
            new XAttribute("IndexX", room.IndexX),
            new XAttribute("IndexY", room.IndexY),
            new XAttribute("Map", room.Id),
            new XAttribute("VNum", room.VNum),
            new XAttribute("XpRate", room.XpRate));

        var discovery = CreateBucketElement("OnCharacterDiscoveringMap", room.Discovery, room.Id);
        if (room.GenerateClock.HasValue)
        {
            discovery ??= new XElement("OnCharacterDiscoveringMap");
            discovery.Add(ValueElement("GenerateClock", room.GenerateClock.Value));
        }
        if (room.GenerateMapClock.HasValue)
        {
            discovery ??= new XElement("OnCharacterDiscoveringMap");
            discovery.Add(ValueElement("GenerateMapClock", room.GenerateMapClock.Value));
        }
        if (room.StartClock)
        {
            discovery ??= new XElement("OnCharacterDiscoveringMap");
            discovery.Add(new XElement("StartClock"));
        }
        if (room.StartMapClock)
        {
            discovery ??= new XElement("OnCharacterDiscoveringMap");
            discovery.Add(new XElement("StartMapClock"));
        }
        if (discovery is not null)
        {
            element.Add(discovery);
        }

        var move = CreateBucketElement("OnMoveOnMap", room.Move, room.Id);
        if (room.Clean.HasContent)
        {
            move ??= new XElement("OnMoveOnMap");
            var clean = CreateBucketElement("OnMapClean", room.Clean, room.Id);
            if (clean is not null)
            {
                move.Add(clean);
            }
        }
        if (move is not null)
        {
            element.Add(move);
        }

        foreach (var button in room.Buttons.OrderBy(static button => button.Id))
        {
            var buttonElement = new XElement(
                "SpawnButton",
                new XAttribute("Id", button.Id),
                new XAttribute("PositionX", button.PositionX),
                new XAttribute("PositionY", button.PositionY),
                new XAttribute("VNumDisabled", button.VNumDisabled),
                new XAttribute("VNumEnabled", button.VNumEnabled));
            var firstEnable = CreateBucketElement("OnFirstEnable", room.FirstEnable, room.Id);
            if (firstEnable is not null)
            {
                buttonElement.Add(firstEnable);
            }
            element.Add(buttonElement);
        }

        foreach (var portal in room.Portals.OrderBy(static portal => portal.IdOnMap))
        {
            var portalElement = new XElement(
                "SpawnPortal",
                new XAttribute("IdOnMap", portal.IdOnMap),
                new XAttribute("PositionX", portal.PositionX),
                new XAttribute("PositionY", portal.PositionY),
                new XAttribute("ToMap", portal.ToMap),
                new XAttribute("ToX", portal.ToX),
                new XAttribute("ToY", portal.ToY),
                new XAttribute("Type", portal.Type));
            if (portal.EndsInstance)
            {
                portalElement.Add(new XElement("OnTraversal", new XElement("End", new XAttribute("Type", 5))));
            }
            element.Add(portalElement);
        }

        return element;
    }

    private static XElement? CreateBucketElement(string name, EventBucket bucket, int roomId)
    {
        if (!bucket.HasContent)
        {
            return null;
        }

        var element = new XElement(name);
        foreach (var clock in bucket.GeneratedClocks)
        {
            element.Add(ValueElement("GenerateClock", clock));
        }
        foreach (var dialog in bucket.Dialogs)
        {
            element.Add(ValueElement("NpcDialog", dialog));
        }
        foreach (var change in bucket.PortalTypeChanges)
        {
            element.Add(new XElement(
                "ChangePortalType",
                new XAttribute("IdOnMap", change.PortalId),
                new XAttribute("Map", roomId),
                new XAttribute("Type", change.Type)));
        }
        if (bucket.RefreshMapItems)
        {
            element.Add(new XElement("RefreshMapItems"));
        }
        foreach (var message in bucket.Messages)
        {
            element.Add(new XElement("SendMessage", new XAttribute("Type", message.Type), new XAttribute("Value", message.Value)));
        }
        foreach (var packet in bucket.Packets)
        {
            element.Add(new XElement("SendPacket", new XAttribute("Value", packet)));
        }
        foreach (var monster in bucket.Monsters)
        {
            element.Add(CreateMonsterElement(monster, roomId));
        }
        foreach (var npc in bucket.Npcs)
        {
            element.Add(new XElement(
                "SummonNpc",
                new XAttribute("Dir", npc.Direction),
                new XAttribute("IsHostile", npc.IsHostile),
                new XAttribute("IsProtected", npc.IsProtected),
                new XAttribute("Move", npc.Move),
                new XAttribute("PositionX", npc.PositionX),
                new XAttribute("PositionY", npc.PositionY),
                new XAttribute("VNum", npc.VNum)));
        }
        if (bucket.OnMapClean && name != "OnMapClean")
        {
            element.Add(new XElement("OnMapClean"));
        }

        return element;
    }

    private static XElement CreateMonsterElement(MonsterDefinition monster, int roomId)
    {
        var element = new XElement(
            "SummonMonster",
            new XAttribute("IsBonus", monster.IsBonus),
            new XAttribute("IsHostile", monster.IsHostile),
            new XAttribute("IsTarget", monster.IsTarget),
            new XAttribute("Move", monster.Move),
            new XAttribute("PositionX", monster.PositionX),
            new XAttribute("PositionY", monster.PositionY),
            new XAttribute("VNum", monster.VNum));
        var onDeath = CreateBucketElement("OnDeath", monster.OnDeath, roomId);
        if (onDeath is not null)
        {
            element.Add(onDeath);
        }
        return element;
    }

    private static void AddItems(XElement globals, string name, IReadOnlyCollection<RewardItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        globals.Add(new XElement(
            name,
            items.Select(static item => new XElement(
                "Item",
                new XAttribute("Amount", item.Amount),
                new XAttribute("VNum", item.VNum)))));
    }

    private static XElement ValueElement(string name, object value) => new(name, new XAttribute("Value", value));

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => System.Text.Encoding.UTF8;
    }
}

internal static class TimeSpaceValidator
{
    public static ValidationResult Validate(TimeSpaceDefinition definition)
    {
        var result = new ValidationResult();
        if (definition.Rooms.Count == 0)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "NO_ROOMS", "The capture did not produce any CreateMap rooms."));
            return result;
        }

        if (definition.Globals.LevelMinimum > definition.Globals.LevelMaximum)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "LEVEL_RANGE_INVALID", "LevelMinimum is greater than LevelMaximum."));
        }

        foreach (var duplicate in definition.Rooms.GroupBy(static room => room.RuntimeMapId).Where(static group => group.Count() > 1))
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "DUPLICATE_RUNTIME_MAP", $"Runtime map id {duplicate.Key} appears more than once."));
        }

        var hasEndPortal = false;
        foreach (var room in definition.Rooms)
        {
            if (room.VNum <= 0)
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "MAP_VNUM_INVALID", $"Room {room.Id} has invalid map VNum {room.VNum}."));
            }

            foreach (var duplicatePortal in room.Portals.GroupBy(static portal => portal.IdOnMap).Where(static group => group.Count() > 1))
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "DUPLICATE_PORTAL_ID", $"Room {room.Id} contains duplicate portal id {duplicatePortal.Key}."));
            }

            foreach (var portal in room.Portals)
            {
                hasEndPortal |= portal.EndsInstance;
                if (!portal.EndsInstance && (portal.ToMap < 0 || portal.ToMap >= definition.Rooms.Count))
                {
                    result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "PORTAL_TARGET_INVALID", $"Room {room.Id} portal {portal.IdOnMap} targets missing room {portal.ToMap}."));
                }
                if (portal.DestinationWasInferred)
                {
                    result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "PORTAL_TARGET_REVIEW", $"Room {room.Id} portal {portal.IdOnMap} uses an inferred destination and must be reviewed."));
                }
            }
        }

        if (!hasEndPortal)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "NO_END_PORTAL", "No explicit end portal was detected."));
        }

        return result;
    }

    public static ValidationResult ValidateXml(string xmlPath)
    {
        var result = new ValidationResult();
        XDocument document;
        try
        {
            document = XDocument.Load(xmlPath, LoadOptions.SetLineInfo);
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "XML_READ_FAILED", exception.Message));
            return result;
        }

        var root = document.Root;
        if (root?.Name.LocalName != "Definition")
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "ROOT_INVALID", "The XML root must be Definition."));
            return result;
        }

        if (root.Element("Globals") is null)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "GLOBALS_MISSING", "The XML has no Globals element."));
        }

        var createMaps = root.Element("InstanceEvents")?.Elements("CreateMap").ToList() ?? new List<XElement>();
        if (createMaps.Count == 0)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CREATE_MAP_MISSING", "The XML has no InstanceEvents/CreateMap elements."));
        }

        var mapIds = new HashSet<int>();
        foreach (var map in createMaps)
        {
            if (!TryAttributeInt(map, "Map", out var mapId) || !mapIds.Add(mapId))
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "MAP_ID_INVALID", "Every CreateMap must have a unique integer Map attribute."));
            }
            if (!TryAttributeInt(map, "VNum", out var vnum) || vnum <= 0)
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "MAP_VNUM_INVALID", "Every CreateMap must have a positive VNum attribute."));
            }
        }

        return result;
    }

    private static bool TryAttributeInt(XElement element, string name, out int value) =>
        int.TryParse(element.Attribute(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}

internal static class ReportWriter
{
    public static void WriteParseReports(string outputXml, string sourcePath, ParseResult parseResult, ValidationResult validation)
    {
        var reportBase = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(outputXml)) ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(outputXml));
        var allDiagnostics = parseResult.Diagnostics.Concat(validation.Diagnostics).ToList();
        var sourceHash = ComputeSha256(sourcePath);
        var payload = new
        {
            source = Path.GetFileName(sourcePath),
            sourceSha256 = sourceHash,
            parsedPackets = parseResult.ParsedPacketCount,
            ignoredLines = parseResult.IgnoredLineCount,
            rooms = parseResult.Definition.Rooms.Count,
            monsters = parseResult.Definition.Rooms.Sum(CountMonsters),
            npcs = parseResult.Definition.Rooms.Sum(CountNpcs),
            portals = parseResult.Definition.Rooms.Sum(static room => room.Portals.Count),
            buttons = parseResult.Definition.Rooms.Sum(static room => room.Buttons.Count),
            diagnostics = allDiagnostics.Select(static diagnostic => new
            {
                severity = diagnostic.Severity.ToString(),
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.LineNumber,
                diagnostic.RawLine
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(reportBase + ".report.json", json + Environment.NewLine, new UTF8Encoding(false));

        var markdown = new StringBuilder();
        markdown.AppendLine("# NosGM Time-Space parser report");
        markdown.AppendLine();
        markdown.AppendLine($"- Source: `{Path.GetFileName(sourcePath)}`");
        markdown.AppendLine($"- Source SHA-256: `{sourceHash}`");
        markdown.AppendLine($"- Parsed packets: {parseResult.ParsedPacketCount}");
        markdown.AppendLine($"- Ignored lines: {parseResult.IgnoredLineCount}");
        markdown.AppendLine($"- Rooms: {parseResult.Definition.Rooms.Count}");
        markdown.AppendLine($"- Monsters: {parseResult.Definition.Rooms.Sum(CountMonsters)}");
        markdown.AppendLine($"- NPCs: {parseResult.Definition.Rooms.Sum(CountNpcs)}");
        markdown.AppendLine($"- Portals: {parseResult.Definition.Rooms.Sum(static room => room.Portals.Count)}");
        markdown.AppendLine($"- Buttons: {parseResult.Definition.Rooms.Sum(static room => room.Buttons.Count)}");
        markdown.AppendLine();
        markdown.AppendLine("## Diagnostics");
        markdown.AppendLine();
        if (allDiagnostics.Count == 0)
        {
            markdown.AppendLine("No diagnostics.");
        }
        else
        {
            foreach (var diagnostic in allDiagnostics)
            {
                var line = diagnostic.LineNumber.HasValue ? $" line {diagnostic.LineNumber.Value}" : string.Empty;
                markdown.AppendLine($"- **{diagnostic.Severity} {diagnostic.Code}**{line}: {diagnostic.Message}");
            }
        }
        File.WriteAllText(reportBase + ".report.md", markdown.ToString(), new UTF8Encoding(false));
    }

    public static void WriteValidationReport(string xmlPath, ValidationResult validation)
    {
        var reportPath = xmlPath + ".validation.json";
        var payload = new
        {
            file = Path.GetFileName(xmlPath),
            valid = !validation.HasErrors,
            diagnostics = validation.Diagnostics.Select(static diagnostic => new
            {
                severity = diagnostic.Severity.ToString(),
                diagnostic.Code,
                diagnostic.Message
            })
        };
        File.WriteAllText(reportPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, new UTF8Encoding(false));
    }

    private static int CountMonsters(TimeSpaceRoom room) =>
        room.Discovery.Monsters.Count + room.Move.Monsters.Count + room.FirstEnable.Monsters.Count + room.Clean.Monsters.Count;

    private static int CountNpcs(TimeSpaceRoom room) =>
        room.Discovery.Npcs.Count + room.Move.Npcs.Count + room.FirstEnable.Npcs.Count + room.Clean.Npcs.Count;

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
