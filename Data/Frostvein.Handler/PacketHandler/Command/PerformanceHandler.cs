using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Packets.Packets.CommandPackets;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class PerformanceHandler : IPacketHandler
    {
        public PerformanceHandler(ClientSession session) => Session = session;

        public ClientSession Session { get; }

        public void Performance(PerformancePacket performancePacket)
        {
            if (performancePacket == null || Session?.Character == null)
            {
                return;
            }

            var manager = ServerManager.Instance;
            var sessions = manager.Sessions.ToList();
            var mapInstances = ServerManager._mapinstances.Values.ToArray();

            var monsterCount = mapInstances.Sum(map => map?.Monsters?.Count ?? 0);
            var npcCount = mapInstances.Sum(map => map?.Npcs?.Count ?? 0);
            var mapSessionCount = mapInstances.Sum(map => map?.Sessions?.Count() ?? 0);

            using (var process = Process.GetCurrentProcess())
            {
                process.Refresh();

                var uptime = DateTime.Now - process.StartTime;
                var cpuCapacityMilliseconds = uptime.TotalMilliseconds * Math.Max(1, Environment.ProcessorCount);
                var averageCpuPercent = cpuCapacityMilliseconds <= 0
                    ? 0
                    : process.TotalProcessorTime.TotalMilliseconds / cpuCapacityMilliseconds * 100d;

                SendLine("========== NosGM Performance ==========");
                SendLine($"Channel: {manager.ChannelId} | Uptime: {FormatDuration(uptime)}");
                SendLine($"Players: {sessions.Count} | Map sessions: {mapSessionCount}");
                SendLine($"Map instances: {mapInstances.Length} | Monsters: {monsterCount} | NPCs: {npcCount}");
                SendLine($"Working set: {ToMegabytes(process.WorkingSet64):N1} MB | Private: {ToMegabytes(process.PrivateMemorySize64):N1} MB");
                SendLine($"Managed heap: {ToMegabytes(GC.GetTotalMemory(false)):N1} MB | Threads: {process.Threads.Count} | Handles: {process.HandleCount}");
                SendLine($"GC collections: Gen0 {GC.CollectionCount(0)} | Gen1 {GC.CollectionCount(1)} | Gen2 {GC.CollectionCount(2)}");
                SendLine($"CPU average since start: {averageCpuPercent.ToString("N1", CultureInfo.InvariantCulture)}%");
                SendLine("=======================================");
            }
        }

        private void SendLine(string message)
        {
            Session.SendPacket(Session.Character.GenerateSay(message, 10));
        }

        private static double ToMegabytes(long bytes) => bytes / 1024d / 1024d;

        private static string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours:00}h {duration.Minutes:00}m {duration.Seconds:00}s";
        }
    }
}
