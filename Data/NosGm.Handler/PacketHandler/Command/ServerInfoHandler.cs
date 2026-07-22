using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Core.Diagnostics;
using NosGm.Core.Handling;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ServerInfoHandler : IPacketHandler
    {
        public ServerInfoHandler(ClientSession session)
        {
            Session = session;
            GmCommandAuditBootstrap.EnsureConfigured();
        }

        public ClientSession Session { get; }

        public void ServerInfo(ServerInfoPacket serverInfoPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("------------Server Info------------", 11));
            long actualChannelId = 0;

            CommunicationServiceClient.Instance.GetOnlineCharacters()
                .Where(s => serverInfoPacket.ChannelId == null || s[1] == serverInfoPacket.ChannelId)
                .OrderBy(s => s[1])
                .ToList()
                .ForEach(s =>
                {
                    if (s[1] > actualChannelId)
                    {
                        if (actualChannelId > 0)
                        {
                            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
                        }

                        actualChannelId = s[1];
                        Session.SendPacket(Session.Character.GenerateSay($"-------------Channel:{actualChannelId}-------------", 11));
                    }

                    var character = DAOFactory.CharacterDAO.LoadById(s[0]);
                    Session.SendPacket(Session.Character.GenerateSay(
                        $"CharacterName: {character.Name} | CharacterId: {character.CharacterId} | SessionId: {s[2]}", 12));
                });

            Session.SendPacket(Session.Character.GenerateSay("----------------------------------------", 11));
        }

        public void Performance(PerformancePacket performancePacket)
        {
            if (performancePacket == null || Session?.Character == null)
            {
                return;
            }

            string[] arguments = (performancePacket.Mode ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mode = arguments.FirstOrDefault()?.ToLowerInvariant() ?? "runtime";

            switch (mode)
            {
                case "packets":
                case "packet":
                    ShowPacketMetrics(arguments.Skip(1).FirstOrDefault());
                    break;

                case "maps":
                case "map":
                    ShowMapMetrics();
                    break;

                case "security":
                case "guard":
                case "flood":
                    ShowSecurityMetrics();
                    break;

                case "reset":
                    ServerPerformanceMonitor.Instance.Reset();
                    PacketSecurityMonitor.Instance.Reset();
                    SendPerformanceLine("NosGM performance and packet guard counters were reset.", 11);
                    Logger.LogUserEvent("PERF_RESET", Session.GenerateIdentity(),
                        "Runtime, network, packet handler and packet guard counters reset.");
                    break;

                case "help":
                case "?":
                    ShowPerformanceHelp();
                    break;

                case "runtime":
                case "summary":
                default:
                    ShowRuntimeSummary();
                    break;
            }
        }

        public void GmAudit(GmAuditPacket packet)
        {
            if (packet == null || Session?.Character == null)
            {
                SendAuditHelp();
                return;
            }

            string[] arguments = (packet.Contents ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mode = arguments.FirstOrDefault()?.ToLowerInvariant() ?? "recent";

            switch (mode)
            {
                case "recent":
                case "list":
                    WriteAuditRows(GmCommandAuditService.Instance.GetRecent(ReadTake(arguments, 1)));
                    return;

                case "failed":
                case "errors":
                    WriteAuditRows(GmCommandAuditService.Instance.GetFailed(ReadTake(arguments, 1)));
                    return;

                case "account":
                    ShowAccountAudit(arguments);
                    return;

                case "character":
                case "char":
                    ShowCharacterAudit(arguments);
                    return;

                case "command":
                case "cmd":
                    if (arguments.Length < 2)
                    {
                        SendAuditHelp();
                        return;
                    }
                    WriteAuditRows(GmCommandAuditService.Instance.GetByCommand(
                        arguments[1], ReadTake(arguments, 2)));
                    return;

                case "status":
                    SendAuditLine(
                        GmCommandAuditService.Instance.IsAvailable()
                            ? "GM command audit table is available and recording."
                            : "GM command audit table is missing or unavailable. Apply Database/Migrations/20260720_GmCommandAudit.sql.",
                        GmCommandAuditService.Instance.IsAvailable() ? (byte)10 : (byte)11);
                    return;

                case "help":
                case "?":
                default:
                    SendAuditHelp();
                    return;
            }
        }

        private void ShowAccountAudit(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                SendAuditHelp();
                return;
            }

            long accountId;
            if (!long.TryParse(arguments[1], out accountId))
            {
                AccountDTO account = DAOFactory.AccountDAO.LoadByName(arguments[1]);
                if (account == null)
                {
                    SendAuditLine("Account not found.", 11);
                    return;
                }
                accountId = account.AccountId;
            }

            WriteAuditRows(GmCommandAuditService.Instance.GetByAccountId(
                accountId, ReadTake(arguments, 2)));
        }

        private void ShowCharacterAudit(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                SendAuditHelp();
                return;
            }

            long characterId;
            if (!long.TryParse(arguments[1], out characterId))
            {
                CharacterDTO character = DAOFactory.CharacterDAO.LoadByName(arguments[1]);
                if (character == null)
                {
                    SendAuditLine("Character not found.", 11);
                    return;
                }
                characterId = character.CharacterId;
            }

            WriteAuditRows(GmCommandAuditService.Instance.GetByCharacterId(
                characterId, ReadTake(arguments, 2)));
        }

        private void WriteAuditRows(IEnumerable<GmCommandAuditDTO> source)
        {
            List<GmCommandAuditDTO> rows = source?.ToList() ?? new List<GmCommandAuditDTO>();
            SendAuditLine("===== GM command audit =====", 12);
            if (rows.Count == 0)
            {
                SendAuditLine("No matching audit events were found.", 11);
                return;
            }

            foreach (GmCommandAuditDTO row in rows)
            {
                string actor = !string.IsNullOrWhiteSpace(row.CharacterName)
                    ? row.CharacterName
                    : row.CharacterId?.ToString(CultureInfo.InvariantCulture)
                      ?? row.AccountId?.ToString(CultureInfo.InvariantCulture)
                      ?? "unknown";
                string location = row.MapId.HasValue ? $"map={row.MapId.Value}" : "map=-";
                string command = LimitDisplay(row.CommandText, 150);
                SendAuditLine(
                    $"{row.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z {row.Outcome} {row.CommandHeader} " +
                    $"actor={actor} account={FormatNullable(row.AccountId)} auth={row.Authority} " +
                    $"required={row.RequiredAuthority} ch={row.ChannelId} {location} ip={row.IpAddress ?? "-"} | {command}",
                    row.Outcome == GmCommandAuditOutcome.Failed ? (byte)11 : (byte)10);

                if (!string.IsNullOrWhiteSpace(row.Failure))
                {
                    SendAuditLine($"  failure: {LimitDisplay(row.Failure, 180)}", 11);
                }
            }
        }

        private void SendAuditHelp()
        {
            SendAuditLine(GmAuditPacket.ReturnHelp(), 10);
            SendAuditLine("$GmAudit recent [take]", 10);
            SendAuditLine("$GmAudit failed [take]", 10);
            SendAuditLine("$GmAudit account <AccountId|AccountName> [take]", 10);
            SendAuditLine("$GmAudit character <CharacterId|CharacterName> [take]", 10);
            SendAuditLine("$GmAudit command <$Header> [take]", 10);
            SendAuditLine("$GmAudit status", 10);
        }

        private void ShowRuntimeSummary()
        {
            ServerManager manager = ServerManager.Instance;
            PerformanceSnapshot metrics = ServerPerformanceMonitor.Instance.Capture();
            PacketSecuritySnapshot security = PacketSecurityMonitor.Instance.Capture();
            List<ClientSession> sessions = manager.Sessions.ToList();
            MapInstance[] mapInstances = ServerManager._mapinstances.Values
                .Where(map => map != null)
                .ToArray();

            int selectedCharacters = sessions.Count(session => session?.HasSelectedCharacter == true);
            int mapSessionCount = mapInstances.Sum(map => map.Sessions?.Count() ?? 0);
            int monsterCount = mapInstances.Sum(map => map.Monsters?.Count ?? 0);
            int npcCount = mapInstances.Sum(map => map.Npcs?.Count ?? 0);
            int groupCount = manager.ThreadSafeGroupList?.Count ?? 0;
            int activeRaids = manager.Raids?.Count ?? 0;
            int activeTimespaces = manager.TimeSpaces?.Count ?? 0;

            SendPerformanceLine("========== NosGM Performance ==========", 11);
            SendPerformanceLine(
                $"Channel {manager.ChannelId} | Uptime {FormatDuration(metrics.Uptime)} | CPU {metrics.CpuPercent:N1}%");
            SendPerformanceLine(
                $"Sessions {sessions.Count} | Characters {selectedCharacters} | Map registrations {mapSessionCount}");
            SendPerformanceLine(
                $"Maps {mapInstances.Length} | Monsters {monsterCount} | NPCs {npcCount} | Groups {groupCount}");
            SendPerformanceLine(
                $"Raids {activeRaids} | TimeSpaces {activeTimespaces} | Events {manager.StartedEvents?.Count ?? 0}");
            SendPerformanceLine(
                $"Memory WS {ToMegabytes(metrics.WorkingSetBytes):N1} MB | Private {ToMegabytes(metrics.PrivateBytes):N1} MB | Heap {ToMegabytes(metrics.ManagedHeapBytes):N1} MB");
            SendPerformanceLine(
                $"Peak WS {ToMegabytes(metrics.PeakWorkingSetBytes):N1} MB | Peak heap {ToMegabytes(metrics.PeakManagedHeapBytes):N1} MB");
            SendPerformanceLine(
                $"Network/s IN {metrics.ReceivedPacketsPerSecond} pkt {FormatBytes(metrics.ReceivedBytesPerSecond)} | OUT {metrics.SentPacketsPerSecond} pkt {FormatBytes(metrics.SentBytesPerSecond)}");
            SendPerformanceLine(
                $"Handlers/s {metrics.HandledPacketsPerSecond} | Avg {metrics.HandlerAverageMilliseconds:N3} ms | Max {metrics.HandlerMaximumMilliseconds:N3} ms | Errors {metrics.HandlerErrorsPerSecond}");
            SendPerformanceLine(
                $"Lifetime handlers {metrics.HandledPackets:N0} | Avg {metrics.HandlerLifetimeAverageMilliseconds:N3} ms | Max {metrics.HandlerLifetimeMaximumMilliseconds:N3} ms | Errors {metrics.HandlerErrors:N0}");
            SendPerformanceLine(
                $"Packet guard blocked transport {security.TransportBlocked:N0} | handlers {security.HandlerBlocked:N0} | disconnects {security.Disconnects:N0}");
            SendPerformanceLine(
                $"Threads {metrics.ProcessThreads} | Handles {metrics.HandleCount} | Pool worker {metrics.ThreadPoolBusyWorker}/{metrics.ThreadPoolMaximumWorker} | IO {metrics.ThreadPoolBusyIo}/{metrics.ThreadPoolMaximumIo}");
            SendPerformanceLine(
                $"GC Gen0 {metrics.Gen0Collections} | Gen1 {metrics.Gen1Collections} | Gen2 {metrics.Gen2Collections}");

            string health = BuildHealthSummary(metrics, security, sessions.Count, mapInstances.Length);
            SendPerformanceLine($"Health: {health}", health == "OK" ? (byte)10 : (byte)12);
            SendPerformanceLine("Use $Perf packets, $Perf maps, $Perf security or $Perf help.", 11);
        }

        private void ShowPacketMetrics(string sortArgument)
        {
            HandlerSort sort = ParseHandlerSort(sortArgument);
            IReadOnlyList<HandlerPerformanceSnapshot> handlers =
                ServerPerformanceMonitor.Instance.GetTopHandlers(12, sort);

            SendPerformanceLine($"===== Packet handlers by {sort.ToString()} =====", 11);
            if (handlers.Count == 0)
            {
                SendPerformanceLine("No handler samples have been collected yet.");
                return;
            }

            int position = 1;
            foreach (HandlerPerformanceSnapshot metric in handlers)
            {
                SendPerformanceLine(
                    $"{position++}. {metric.Header} | Calls {metric.Count:N0} | Avg {metric.AverageMilliseconds:N3} ms | Max {metric.MaximumMilliseconds:N3} ms | Total {metric.TotalMilliseconds:N1} ms | Err {metric.Errors:N0}");
            }
        }

        private void ShowMapMetrics()
        {
            var maps = ServerManager._mapinstances.Values
                .Where(map => map != null)
                .Select(map => new
                {
                    Map = map,
                    Sessions = map.Sessions?.Count() ?? 0,
                    Monsters = map.Monsters?.Count ?? 0,
                    Npcs = map.Npcs?.Count ?? 0
                })
                .OrderByDescending(entry => entry.Sessions)
                .ThenByDescending(entry => entry.Monsters + entry.Npcs)
                .Take(12)
                .ToList();

            SendPerformanceLine("===== Hottest map instances =====", 11);
            if (maps.Count == 0)
            {
                SendPerformanceLine("No map instances are currently loaded.");
                return;
            }

            int position = 1;
            foreach (var entry in maps)
            {
                SendPerformanceLine(
                    $"{position++}. Map {entry.Map.Map?.MapId ?? 0} | Type {entry.Map.MapInstanceType} | Players {entry.Sessions} | Monsters {entry.Monsters} | NPCs {entry.Npcs} | Id {entry.Map.MapInstanceId}");
            }
        }

        private void ShowSecurityMetrics()
        {
            PacketSecuritySnapshot security = PacketSecurityMonitor.Instance.Capture();
            IReadOnlyList<PacketSecurityBlockSnapshot> blocked =
                PacketSecurityMonitor.Instance.GetTopBlocked(12);

            SendPerformanceLine("===== Packet guard =====", 11);
            SendPerformanceLine(
                $"Accepted transport {security.TransportAccepted:N0} | handlers {security.HandlerAccepted:N0}");
            SendPerformanceLine(
                $"Blocked transport {security.TransportBlocked:N0} | handlers {security.HandlerBlocked:N0}");
            SendPerformanceLine(
                $"Oversized {security.OversizedMessages:N0} | Dropped {FormatBytes(security.DroppedBytes)} | Disconnects {security.Disconnects:N0}");

            if (blocked.Count == 0)
            {
                SendPerformanceLine("No packet guard violations have been recorded.");
                return;
            }

            int position = 1;
            foreach (PacketSecurityBlockSnapshot metric in blocked)
            {
                SendPerformanceLine(
                    $"{position++}. {metric.Key} | Blocks {metric.Count:N0} | Disconnects {metric.Disconnects:N0}");
            }
        }

        private void ShowPerformanceHelp()
        {
            SendPerformanceLine("========== $Perf help ==========", 11);
            SendPerformanceLine("$Perf or $Perf runtime: process, memory, network and world summary.");
            SendPerformanceLine("$Perf packets: handlers ordered by total execution time.");
            SendPerformanceLine("$Perf packets count: handlers with the most calls.");
            SendPerformanceLine("$Perf packets avg: handlers with the highest average latency.");
            SendPerformanceLine("$Perf packets max: handlers with the slowest single call.");
            SendPerformanceLine("$Perf packets errors: handlers with the most exceptions.");
            SendPerformanceLine("$Perf maps: map instances ordered by players and entities.");
            SendPerformanceLine("$Perf security: packet floods, blocked handlers and disconnects.");
            SendPerformanceLine("$Perf reset: clear performance, security and peak counters.");
        }

        private static HandlerSort ParseHandlerSort(string argument)
        {
            switch (argument?.Trim().ToLowerInvariant())
            {
                case "count":
                case "calls":
                    return HandlerSort.Count;
                case "avg":
                case "average":
                    return HandlerSort.AverageTime;
                case "max":
                case "slow":
                    return HandlerSort.MaximumTime;
                case "error":
                case "errors":
                    return HandlerSort.Errors;
                default:
                    return HandlerSort.TotalTime;
            }
        }

        private static string BuildHealthSummary(
            PerformanceSnapshot metrics,
            PacketSecuritySnapshot security,
            int sessionCount,
            int mapCount)
        {
            var warnings = new List<string>();
            if (metrics.CpuPercent >= 85)
            {
                warnings.Add("CPU HIGH");
            }
            if (metrics.HandlerMaximumMilliseconds >= 100)
            {
                warnings.Add("SLOW HANDLER");
            }
            if (metrics.HandlerErrorsPerSecond > 0)
            {
                warnings.Add("HANDLER ERRORS");
            }
            if (metrics.ThreadPoolMaximumWorker > 0 &&
                metrics.ThreadPoolBusyWorker >= metrics.ThreadPoolMaximumWorker * 0.85)
            {
                warnings.Add("THREADPOOL PRESSURE");
            }
            if (sessionCount > 0 && mapCount == 0)
            {
                warnings.Add("NO MAPS");
            }
            if (security.Disconnects > 0)
            {
                warnings.Add("PACKET FLOOD");
            }

            return warnings.Count == 0 ? "OK" : string.Join(" | ", warnings);
        }

        private void SendPerformanceLine(string message, byte type = 10) =>
            Session.SendPacket(Session.Character.GenerateSay(message, type));

        private void SendAuditLine(string message, byte type = 10) =>
            Session.SendPacket(Session.Character.GenerateSay(message, type));

        private static int ReadTake(string[] arguments, int index, int defaultValue = 15)
        {
            if (arguments.Length <= index || !int.TryParse(arguments[index], out int take))
            {
                return defaultValue;
            }
            return Math.Max(1, Math.Min(50, take));
        }

        private static string LimitDisplay(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            string normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength) + "...";
        }

        private static string FormatNullable(long? value) =>
            value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "-";

        private static double ToMegabytes(long bytes) => bytes / 1024d / 1024d;

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L)
            {
                return $"{bytes / 1024d / 1024d:N2} MB";
            }
            if (bytes >= 1024L)
            {
                return $"{bytes / 1024d:N1} KB";
            }
            return $"{bytes} B";
        }

        private static string FormatDuration(TimeSpan duration) =>
            $"{(int)duration.TotalDays}d {duration.Hours:00}h {duration.Minutes:00}m {duration.Seconds:00}s";
    }

    internal static class GmCommandAuditBootstrap
    {
        private static int _configured;

        public static void EnsureConfigured()
        {
            if (Interlocked.Exchange(ref _configured, 1) == 0)
            {
                GmCommandAuditBridge.Configure(Record);
            }
        }

        private static void Record(GmCommandExecutionEvent auditEvent)
        {
            try
            {
                ClientSession session = ResolveSession(auditEvent?.ParentHandler);
                if (session == null)
                {
                    return;
                }

                Character character = session.HasSelectedCharacter ? session.Character : null;
                PacketDefinition packet = auditEvent.Packet as PacketDefinition;
                string commandText = packet?.OriginalContent ?? packet?.OriginalHeader ?? auditEvent.Header;
                string ipAddress = null;
                try
                {
                    ipAddress = session.CleanIpAddress;
                }
                catch
                {
                    ipAddress = session.IpAddress;
                }

                GmCommandAuditService.Instance.Record(
                    session.Account?.AccountId,
                    character?.CharacterId,
                    character?.Name,
                    session.Account?.Authority ?? AuthorityType.User,
                    auditEvent.Header,
                    commandText,
                    auditEvent.RequiredAuthority,
                    auditEvent.Outcome,
                    ipAddress,
                    ServerManager.Instance.ChannelId,
                    character?.MapId,
                    session.SessionId,
                    auditEvent.Exception);
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to record the GM command execution event.", exception);
            }
        }

        private static ClientSession ResolveSession(object parentHandler)
        {
            if (parentHandler == null) return null;
            PropertyInfo property = parentHandler.GetType().GetProperty("Session");
            return property?.GetValue(parentHandler) as ClientSession;
        }
    }
}
