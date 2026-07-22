// SPDX-License-Identifier: GPL-3.0-only
// Derived from Elendan/TimeSpace-Generator, the SEOVA adaptation,
// noszanou/OpennosTimeSpaceParser and the OpenNos XML model.
// Modifications Copyright (C) 2026 NosGM contributors.

using System.Globalization;

namespace NosGM.TimeSpaceParser;

internal sealed class CaptureParser
{
    private readonly ParseResult _result = new();
    private readonly Dictionary<int, MonsterDefinition> _monstersByEntityId = new();
    private TimeSpaceRoom? _currentRoom;
    private MonsterDefinition? _lastDeadMonster;
    private CapturePhase _phase = CapturePhase.Initial;
    private short _lastPositionX;
    private short _lastPositionY;
    private bool _expectingDescription;
    private int _buttonId;

    public ParseResult Parse(IEnumerable<string> lines, CliOptions options, string sourceName)
    {
        var lineNumber = 0;
        foreach (var originalLine in lines)
        {
            lineNumber++;
            var line = originalLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (_expectingDescription && !LooksLikePacket(line))
            {
                _result.Definition.Globals.Label = line;
                _expectingDescription = false;
                continue;
            }

            _expectingDescription = false;
            try
            {
                ProcessLine(line, lineNumber);
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
            {
                AddDiagnostic(DiagnosticSeverity.Warning, "PACKET_PARSE_FAILED", exception.Message, lineNumber, originalLine);
                _result.IgnoredLineCount++;
            }
        }

        ResolvePortalDestinations();
        ApplyOverrides(options, sourceName);
        return _result;
    }

    private void ProcessLine(string line, int lineNumber)
    {
        var header = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        switch (header)
        {
            case "rbr":
                ParseRbr(line, lineNumber);
                break;
            case "at":
                ParseAt(line, lineNumber);
                break;
            case "rsfn":
            case "rsfm":
                ParseRoomIndex(line, lineNumber);
                break;
            case "walk":
                ParseWalk(line, lineNumber);
                break;
            case "in":
                ParseEntity(line, lineNumber);
                break;
            case "su":
                ParseSkillUse(line, lineNumber);
                break;
            case "gp":
                ParsePortal(line, lineNumber);
                break;
            case "msg":
                ParseMessage(line, lineNumber);
                break;
            case "npc_req":
                ParseDialog(line, lineNumber);
                break;
            case "evnt":
                ParseEvent(line, lineNumber);
                break;
            case "out":
                ParseOut(line, lineNumber);
                break;
            case "preq":
                _phase = CapturePhase.Active;
                _lastDeadMonster = null;
                _result.ParsedPacketCount++;
                break;
            case "eff":
                ParseEffect(line, lineNumber);
                break;
            case "mapclear":
            case "mapclean":
                ParseMapClean(lineNumber);
                break;
            case "sinfo":
            case "minfo":
            case "msgi":
                StoreRawPacket(line, lineNumber);
                break;
            default:
                AddDiagnostic(DiagnosticSeverity.Warning, "UNSUPPORTED_LINE", $"Unsupported capture line header '{header}'.", lineNumber, line);
                _result.IgnoredLineCount++;
                break;
        }
    }

    private void ParseRbr(string line, int lineNumber)
    {
        var parts = Split(line);
        if (parts.Length < 19)
        {
            throw new ArgumentException($"RBR packet on line {lineNumber} is too short.");
        }

        var levelParts = parts[4].Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (levelParts.Length >= 2)
        {
            _result.Definition.Globals.LevelMinimum = ToByte(ParseInt(levelParts[0], "RBR minimum level"));
            _result.Definition.Globals.LevelMaximum = ToByte(ParseInt(levelParts[1], "RBR maximum level"));
        }

        ParseItemRange(parts, 6, 10, _result.Definition.Globals.DrawItems);
        ParseItemRange(parts, 11, 12, _result.Definition.Globals.SpecialItems);
        ParseItemRange(parts, 13, 15, _result.Definition.Globals.GiftItems);

        var nameStart = -1;
        for (var index = 19; index < parts.Length; index++)
        {
            if (!IsNumericOrScore(parts[index]))
            {
                nameStart = index;
                break;
            }
        }

        if (nameStart >= 0)
        {
            _result.Definition.Globals.Name = string.Join(' ', parts.Skip(nameStart));
        }
        else
        {
            AddDiagnostic(DiagnosticSeverity.Warning, "RBR_NAME_MISSING", "No Time-Space name could be extracted from the RBR packet.", lineNumber, line);
        }

        _expectingDescription = true;
        _result.ParsedPacketCount++;
    }

    private void ParseAt(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 9, "AT", lineNumber);

        var runtimeMapId = ParseInt(parts[1], "AT runtime map id");
        var vnum = ToShort(ParseInt(parts[2], "AT map VNum"));
        var positionX = ToShort(ParseInt(parts[3], "AT position X"));
        var positionY = ToShort(ParseInt(parts[4], "AT position Y"));

        _currentRoom = new TimeSpaceRoom
        {
            Id = _result.Definition.Rooms.Count,
            RuntimeMapId = runtimeMapId,
            VNum = vnum,
            EntryX = positionX,
            EntryY = positionY
        };
        _result.Definition.Rooms.Add(_currentRoom);

        if (_result.Definition.Rooms.Count == 1)
        {
            _result.Definition.Globals.StartX = positionX;
            _result.Definition.Globals.StartY = positionY;
        }

        _phase = CapturePhase.Initial;
        _lastDeadMonster = null;
        _lastPositionX = positionX;
        _lastPositionY = positionY;
        _result.ParsedPacketCount++;
    }

    private void ParseRoomIndex(string line, int lineNumber)
    {
        var room = RequireCurrentRoom(lineNumber, line);
        var parts = Split(line);
        RequireLength(parts, 3, parts[0].ToUpperInvariant(), lineNumber);
        room.IndexX = ToByte(ParseInt(parts[1], "room index X"));
        room.IndexY = ToByte(ParseInt(parts[2], "room index Y"));
        _result.ParsedPacketCount++;
    }

    private void ParseWalk(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 5, "WALK", lineNumber);
        _lastPositionX = ToShort(ParseInt(parts[1], "WALK position X"));
        _lastPositionY = ToShort(ParseInt(parts[2], "WALK position Y"));
        _result.ParsedPacketCount++;
    }

    private void ParseEntity(string line, int lineNumber)
    {
        var room = RequireCurrentRoom(lineNumber, line);
        var parts = Split(line);
        RequireLength(parts, 7, "IN", lineNumber);
        var type = ParseInt(parts[1], "IN entity type");

        int vnum;
        int entityId;
        int positionX;
        int positionY;
        int direction;

        if (type == 9)
        {
            RequireLength(parts, 7, "IN object", lineNumber);
            vnum = ParseInt(parts[2], "object VNum");
            entityId = ParseInt(parts[3], "object entity id");
            positionX = ParseInt(parts[4], "object position X");
            positionY = ParseInt(parts[5], "object position Y");
            direction = ParseInt(parts[6], "object direction");
        }
        else
        {
            var dataIndex = FindNumericRun(parts, 2, 5);
            if (dataIndex < 0)
            {
                throw new ArgumentException($"IN packet on line {lineNumber} has no recognizable entity data block.");
            }

            vnum = ParseInt(parts[dataIndex], "entity VNum");
            entityId = ParseInt(parts[dataIndex + 1], "entity id");
            positionX = ParseInt(parts[dataIndex + 2], "entity position X");
            positionY = ParseInt(parts[dataIndex + 3], "entity position Y");
            direction = ParseInt(parts[dataIndex + 4], "entity direction");
        }

        if (type == 2)
        {
            SelectSpawnBucket().Npcs.Add(new NpcDefinition
            {
                VNum = ToShort(vnum),
                EntityId = entityId,
                PositionX = ToShort(positionX),
                PositionY = ToShort(positionY),
                Direction = ToByte(direction)
            });
        }
        else if (type == 3)
        {
            var monster = new MonsterDefinition
            {
                VNum = ToShort(vnum),
                EntityId = entityId,
                PositionX = ToShort(positionX),
                PositionY = ToShort(positionY)
            };
            SelectSpawnBucket().Monsters.Add(monster);
            _monstersByEntityId[entityId] = monster;
        }
        else if (type == 9)
        {
            if (room.SeenButtonEntityIds.Add(entityId))
            {
                var disabled = ToShort(vnum);
                var enabled = vnum switch
                {
                    1000 => (short)1045,
                    1057 => (short)1057,
                    _ => ToShort(vnum + 1)
                };
                room.Buttons.Add(new ButtonDefinition
                {
                    Id = _buttonId++,
                    PositionX = ToShort(positionX),
                    PositionY = ToShort(positionY),
                    VNumEnabled = enabled,
                    VNumDisabled = disabled
                });
            }
        }
        else
        {
            AddDiagnostic(DiagnosticSeverity.Info, "ENTITY_IGNORED", $"Entity type {type} is not needed for Time-Space XML generation.", lineNumber, line);
        }

        _result.ParsedPacketCount++;
    }

    private void ParseSkillUse(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 15, "SU", lineNumber);
        var type = ParseInt(parts[1], "SU type");
        var targetId = ParseInt(parts[4], "SU target id");
        var isAlive = ParseInt(parts[11], "SU alive flag");

        if (type == 1 && isAlive == 0 && _monstersByEntityId.TryGetValue(targetId, out var monster))
        {
            monster.IsDead = true;
            _lastDeadMonster = monster;
        }

        _result.ParsedPacketCount++;
    }

    private void ParsePortal(string line, int lineNumber)
    {
        var room = RequireCurrentRoom(lineNumber, line);
        var parts = Split(line);
        RequireLength(parts, 6, "GP", lineNumber);
        var sourceX = ParseInt(parts[1], "GP source X");
        var sourceY = ParseInt(parts[2], "GP source Y");
        var destinationMapId = ParseInt(parts[3], "GP destination map id");
        var type = ParseInt(parts[4], "GP type");
        var portalId = ParseInt(parts[5], "GP portal id");
        var portalKey = string.Create(CultureInfo.InvariantCulture, $"{portalId}:{sourceX}:{sourceY}:{destinationMapId}:{type}");

        if (!room.SeenPortalKeys.Add(portalKey))
        {
            return;
        }

        if (type == 5 || destinationMapId == -1)
        {
            room.Portals.Add(new PortalDefinition
            {
                IdOnMap = ToByte(portalId),
                PositionX = ToShort(sourceX),
                PositionY = ToShort(sourceY),
                Type = ToShort(type),
                DestinationRuntimeMapId = -1,
                ToMap = -1,
                ToX = ToShort(sourceX),
                ToY = ToShort(sourceY),
                EndsInstance = true
            });
        }
        else if (type >= 2)
        {
            var bucket = _phase == CapturePhase.FirstEnable ? room.FirstEnable : room.Clean;
            bucket.PortalTypeChanges.Add(new PortalTypeChange(portalId, unchecked((sbyte)type)));
            bucket.RefreshMapItems = true;
        }
        else
        {
            room.Portals.Add(new PortalDefinition
            {
                IdOnMap = ToByte(portalId),
                PositionX = ToShort(sourceX),
                PositionY = ToShort(sourceY),
                Type = ToShort(type),
                DestinationRuntimeMapId = destinationMapId,
                ToX = ToShort(sourceX),
                ToY = ToShort(sourceY == 1 ? 28 : sourceY == 28 ? 1 : sourceY)
            });
        }

        _result.ParsedPacketCount++;
    }

    private void ParseMessage(string line, int lineNumber)
    {
        var firstSpace = line.IndexOf(' ');
        var secondSpace = firstSpace < 0 ? -1 : line.IndexOf(' ', firstSpace + 1);
        if (secondSpace < 0)
        {
            throw new ArgumentException($"MSG packet on line {lineNumber} is too short.");
        }

        var typeText = line[(firstSpace + 1)..secondSpace];
        var message = line[(secondSpace + 1)..].Trim();
        SelectNonSpawnBucket(lineNumber, line).Messages.Add(new MessageDefinition(ToByte(ParseInt(typeText, "MSG type")), message));
        _result.ParsedPacketCount++;
    }

    private void ParseDialog(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 4, "NPC_REQ", lineNumber);
        SelectNonSpawnBucket(lineNumber, line).Dialogs.Add(ParseInt(parts[3], "NPC_REQ dialog id"));
        _result.ParsedPacketCount++;
    }

    private void ParseEvent(string line, int lineNumber)
    {
        var room = RequireCurrentRoom(lineNumber, line);
        var parts = Split(line);
        RequireLength(parts, 5, "EVNT", lineNumber);
        var type = ParseInt(parts[1], "EVNT type");
        var time1 = ParseInt(parts[3], "EVNT time 1");
        var time2 = ParseInt(parts[4], "EVNT time 2");

        if (time1 != time2)
        {
            AddDiagnostic(DiagnosticSeverity.Warning, "CLOCK_VALUES_DIFFER", "EVNT clock values differ; the first value was used.", lineNumber, line);
        }

        if (type == 1)
        {
            if (_phase == CapturePhase.Initial)
            {
                room.GenerateClock = time1;
                room.StartClock = true;
            }
            else
            {
                room.Move.GeneratedClocks.Add(time1);
            }
        }
        else if (type == 3)
        {
            room.GenerateMapClock = time1;
            room.StartMapClock = true;
        }
        else
        {
            AddDiagnostic(DiagnosticSeverity.Info, "EVNT_TYPE_IGNORED", $"EVNT type {type} is not currently mapped.", lineNumber, line);
        }

        _result.ParsedPacketCount++;
    }

    private void ParseOut(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 2, "OUT", lineNumber);
        if (ParseInt(parts[1], "OUT type") == 9)
        {
            _phase = CapturePhase.FirstEnable;
            _lastDeadMonster = null;
        }
        _result.ParsedPacketCount++;
    }

    private void ParseEffect(string line, int lineNumber)
    {
        var parts = Split(line);
        RequireLength(parts, 4, "EFF", lineNumber);
        var type = ParseInt(parts[1], "EFF type");
        var entityId = ParseInt(parts[2], "EFF entity id");
        var effectId = ParseInt(parts[3], "EFF effect id");

        if (type == 3 && _monstersByEntityId.TryGetValue(entityId, out var monster))
        {
            if (effectId == 824)
            {
                monster.IsTarget = true;
            }
            else if (effectId == 826)
            {
                monster.IsBonus = true;
            }
        }

        _result.ParsedPacketCount++;
    }

    private void ParseMapClean(int lineNumber)
    {
        var room = RequireCurrentRoom(lineNumber, "mapclean");
        if (_phase == CapturePhase.Initial)
        {
            _phase = CapturePhase.Active;
        }
        else if (_phase == CapturePhase.FirstEnable)
        {
            room.FirstEnable.OnMapClean = true;
            _phase = CapturePhase.Active;
        }
        else
        {
            room.Clean.OnMapClean = true;
        }

        _lastDeadMonster = null;
        _result.ParsedPacketCount++;
    }

    private void StoreRawPacket(string line, int lineNumber)
    {
        SelectNonSpawnBucket(lineNumber, line).Packets.Add(line);
        _result.ParsedPacketCount++;
    }

    private EventBucket SelectSpawnBucket()
    {
        if (_currentRoom is null)
        {
            throw new InvalidOperationException("No current room is available.");
        }

        if (_phase == CapturePhase.Initial)
        {
            return _currentRoom.Discovery;
        }

        if (_phase == CapturePhase.FirstEnable)
        {
            return _currentRoom.FirstEnable;
        }

        return _lastDeadMonster?.OnDeath ?? _currentRoom.Move;
    }

    private EventBucket SelectNonSpawnBucket(int lineNumber, string line)
    {
        var room = RequireCurrentRoom(lineNumber, line);
        if (_phase == CapturePhase.Initial)
        {
            return room.Discovery;
        }

        if (_phase == CapturePhase.FirstEnable)
        {
            return room.FirstEnable;
        }

        return _lastDeadMonster?.OnDeath ?? room.Clean;
    }

    private void ResolvePortalDestinations()
    {
        var byRuntimeMap = _result.Definition.Rooms
            .GroupBy(static room => room.RuntimeMapId)
            .ToDictionary(static group => group.Key, static group => group.First().Id);

        foreach (var room in _result.Definition.Rooms)
        {
            foreach (var portal in room.Portals.Where(static portal => !portal.EndsInstance))
            {
                if (byRuntimeMap.TryGetValue(portal.DestinationRuntimeMapId, out var mappedRoomId))
                {
                    portal.ToMap = ToShort(mappedRoomId);
                    continue;
                }

                if (portal.DestinationRuntimeMapId >= 0 && portal.DestinationRuntimeMapId < _result.Definition.Rooms.Count)
                {
                    portal.ToMap = ToShort(portal.DestinationRuntimeMapId);
                    continue;
                }

                var inferred = portal.PositionY switch
                {
                    1 => room.Id + 1,
                    28 => room.Id - 1,
                    _ => room.Id
                };

                if (inferred < 0 || inferred >= _result.Definition.Rooms.Count)
                {
                    inferred = room.Id;
                }

                portal.ToMap = ToShort(inferred);
                portal.DestinationWasInferred = true;
                AddDiagnostic(
                    DiagnosticSeverity.Warning,
                    "PORTAL_DESTINATION_INFERRED",
                    $"Room {room.Id} portal {portal.IdOnMap} destination could not be matched to runtime map {portal.DestinationRuntimeMapId}; room {inferred} was inferred.");
            }
        }
    }

    private void ApplyOverrides(CliOptions options, string sourceName)
    {
        if (string.Equals(_result.Definition.Globals.Name, "Unnamed Time-Space", StringComparison.Ordinal))
        {
            _result.Definition.Globals.Name = Path.GetFileNameWithoutExtension(sourceName);
        }

        if (!string.IsNullOrWhiteSpace(options.NameOverride))
        {
            _result.Definition.Globals.Name = options.NameOverride;
        }

        if (!string.IsNullOrWhiteSpace(options.LabelOverride))
        {
            _result.Definition.Globals.Label = options.LabelOverride;
        }

        if (options.LivesOverride.HasValue)
        {
            _result.Definition.Globals.Lives = options.LivesOverride.Value;
        }

        if (options.GoldOverride.HasValue)
        {
            _result.Definition.Globals.Gold = options.GoldOverride.Value;
        }

        if (options.ReputationOverride.HasValue)
        {
            _result.Definition.Globals.Reputation = options.ReputationOverride.Value;
        }
    }

    private TimeSpaceRoom RequireCurrentRoom(int lineNumber, string line)
    {
        if (_currentRoom is null)
        {
            throw new ArgumentException($"Packet on line {lineNumber} appeared before an AT room packet: {line}");
        }

        return _currentRoom;
    }

    private void ParseItemRange(string[] parts, int startIndex, int endIndex, ICollection<RewardItem> destination)
    {
        for (var index = startIndex; index <= endIndex && index < parts.Length; index++)
        {
            var token = parts[index];
            if (token is "-1" or "-1.0")
            {
                continue;
            }

            var itemParts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (itemParts.Length < 2 || !TryParseInt(itemParts[0], out var vnum) || !TryParseInt(itemParts[1], out var amount) || vnum <= 0)
            {
                continue;
            }

            destination.Add(new RewardItem(ToShort(vnum), ToShort(amount)));
        }
    }

    private static int FindNumericRun(string[] parts, int startIndex, int length)
    {
        for (var index = startIndex; index + length <= parts.Length; index++)
        {
            var allNumeric = true;
            for (var offset = 0; offset < length; offset++)
            {
                if (!TryParseInt(parts[index + offset], out _))
                {
                    allNumeric = false;
                    break;
                }
            }

            if (allNumeric)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool LooksLikePacket(string line)
    {
        var header = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        return header.Equals("mapclear", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("mapclean", StringComparison.OrdinalIgnoreCase) ||
               new[] { "rbr", "at", "rsfn", "rsfm", "walk", "in", "su", "gp", "msg", "npc_req", "evnt", "out", "preq", "eff", "sinfo", "minfo", "msgi" }
                   .Contains(header, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsNumericOrScore(string value) =>
        value is "0" or "0." ||
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private static string[] Split(string line) => line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static void RequireLength(string[] parts, int expected, string packetName, int lineNumber)
    {
        if (parts.Length < expected)
        {
            throw new ArgumentException($"{packetName} packet on line {lineNumber} is too short. Expected at least {expected} fields, got {parts.Length}.");
        }
    }

    private static int ParseInt(string value, string field)
    {
        if (!TryParseInt(value, out var parsed))
        {
            throw new FormatException($"Invalid integer for {field}: '{value}'.");
        }

        return parsed;
    }

    private static bool TryParseInt(string value, out int parsed) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

    private static short ToShort(int value) => checked((short)value);
    private static byte ToByte(int value) => checked((byte)value);

    private void AddDiagnostic(DiagnosticSeverity severity, string code, string message, int? lineNumber = null, string? rawLine = null) =>
        _result.Diagnostics.Add(new Diagnostic(severity, code, message, lineNumber, rawLine));
}
