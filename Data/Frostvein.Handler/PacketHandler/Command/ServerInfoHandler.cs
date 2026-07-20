using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Core.Diagnostics;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ServerInfoHandler : IPacketHandler
    {
        public ServerInfoHandler(ClientSession session) => Session = session;

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

                case "reset":
                    ServerPerformanceMonitor.Instance.Reset();
                    SendPerformanceLine("NosGM performance counters were reset.", 11);
                    Logger.LogUserEvent("PERF_RESET", Session.GenerateIdentity(),
                        "Runtime, network and packet handler counters reset.");
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

        private void ShowRuntimeSummary()
        {
            ServerManager manager = ServerManager.Instance;
            PerformanceSnapshot metrics = ServerPerformanceMonitor.Instance.Capture();
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
                $"Threads {metrics.ProcessThreads} | Handles {metrics.HandleCount} | Pool worker {metrics.ThreadPoolBusyWorker}/{metrics.ThreadPoolMaximumWorker} | IO {metrics.ThreadPoolBusyIo}/{metrics.ThreadPoolMaximumIo}");
            SendPerformanceLine(
                $"GC Gen0 {metrics.Gen0Collections} | Gen1 {metrics.Gen1Collections} | Gen2 {metrics.Gen2Collections}");

            string health = BuildHealthSummary(metrics, sessions.Count, mapInstances.Length);
            SendPerformanceLine($"Health: {health}", health == "OK" ? (byte)10 : (byte)12);
            SendPerformanceLine("Use $Perf packets, $Perf maps or $Perf help.", 11);
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
            SendPerformanceLine("$Perf reset: clear lifetime counters and peaks.");
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

            return warnings.Count == 0 ? "OK" : string.Join(" | ", warnings);
        }

        private void SendPerformanceLine(string message, byte type = 10) =>
            Session.SendPacket(Session.Character.GenerateSay(message, type));

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
}
