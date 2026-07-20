using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Diagnostics;
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

                SendPerformanceLine("========== NosGM Performance ==========");
                SendPerformanceLine($"Channel: {manager.ChannelId} | Uptime: {FormatDuration(uptime)}");
                SendPerformanceLine($"Players: {sessions.Count} | Map sessions: {mapSessionCount}");
                SendPerformanceLine($"Map instances: {mapInstances.Length} | Monsters: {monsterCount} | NPCs: {npcCount}");
                SendPerformanceLine($"Working set: {ToMegabytes(process.WorkingSet64):N1} MB | Private: {ToMegabytes(process.PrivateMemorySize64):N1} MB");
                SendPerformanceLine($"Managed heap: {ToMegabytes(GC.GetTotalMemory(false)):N1} MB | Threads: {process.Threads.Count} | Handles: {process.HandleCount}");
                SendPerformanceLine($"GC collections: Gen0 {GC.CollectionCount(0)} | Gen1 {GC.CollectionCount(1)} | Gen2 {GC.CollectionCount(2)}");
                SendPerformanceLine($"CPU average since start: {averageCpuPercent.ToString("N1", CultureInfo.InvariantCulture)}%");
                SendPerformanceLine("=======================================");
            }
        }

        private void SendPerformanceLine(string message) =>
            Session.SendPacket(Session.Character.GenerateSay(message, 10));

        private static double ToMegabytes(long bytes) => bytes / 1024d / 1024d;

        private static string FormatDuration(TimeSpan duration) =>
            $"{(int)duration.TotalDays}d {duration.Hours:00}h {duration.Minutes:00}m {duration.Seconds:00}s";
    }
}
