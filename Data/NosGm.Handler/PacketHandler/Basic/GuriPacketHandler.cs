using NosGm.Core;
using NosGm.Core.Diagnostics;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Plugin.Event;
using NosGm.Packets.Packets.ClientPackets;
using System;
using System.Diagnostics;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class GuriPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GuriPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Guri(GuriPacket guriPacket)
        {
            if (guriPacket == null)
            {
                return;
            }

            long started = Stopwatch.GetTimestamp();
            bool succeeded = false;
            try
            {
                HandleGuri(guriPacket);
                succeeded = true;
            }
            finally
            {
                GuriPerformanceMonitor.Record(
                    guriPacket.Type,
                    Stopwatch.GetTimestamp() - started,
                    succeeded);
            }
        }

        public void GuriPerformance(GuriPerformancePacket packet)
        {
            if (Session?.Character == null)
            {
                return;
            }

            string mode = packet?.Mode?.Trim().ToLowerInvariant() ?? "total";
            if (mode == "reset")
            {
                GuriPerformanceMonitor.Reset();
                SendDiagnostic("Guri performance counters were reset.", 11);
                return;
            }

            HandlerSort sort = ParseSort(mode);
            var metrics = GuriPerformanceMonitor.GetTop(12, sort);

            SendDiagnostic($"========== Guri types by {sort} ==========", 11);
            if (metrics.Count == 0)
            {
                SendDiagnostic("No guri samples have been collected yet.");
                SendDiagnostic(GuriPerformancePacket.ReturnHelp(), 11);
                return;
            }

            int position = 1;
            foreach (GuriPerformanceSnapshot metric in metrics)
            {
                string typeName = ResolveTypeName(metric.Type);
                SendDiagnostic(
                    $"{position++}. Type {metric.Type} ({typeName}) | Calls {metric.Count:N0} | " +
                    $"Avg {metric.AverageMilliseconds:N3} ms | Max {metric.MaximumMilliseconds:N3} ms | " +
                    $"Total {metric.TotalMilliseconds:N1} ms | Missing {metric.MissingHandlers:N0} | Err {metric.Errors:N0}");
            }

            SendDiagnostic(GuriPerformancePacket.ReturnHelp(), 11);
        }

        private void HandleGuri(GuriPacket guriPacket)
        {
            Character character = Session?.Character;
            if (character?.MapInstance == null || Session.CurrentMapInstance == null)
            {
                return;
            }

            if (guriPacket.Type == (long)GuriType.RainbowBattleFlag)
            {
                if (character.isFreezed)
                {
                    return;
                }

                MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == guriPacket.User);
                if (npc == null)
                {
                    return;
                }

                int distance = Map.GetDistance(
                    new MapCell { X = character.PositionX, Y = character.PositionY },
                    new MapCell { X = npc.MapX, Y = npc.MapY });
                if (distance > 5)
                {
                    return;
                }

                var rainbowTeam = ServerManager.Instance.RainbowBattleMembers
                    .Find(team => team.Session.Contains(Session));

                if (rainbowTeam == null ||
                    RainbowBattleManager.AlreadyHaveFlag(
                        rainbowTeam,
                        (RainbowNpcType)guriPacket.Argument,
                        (int)guriPacket.User))
                {
                    return;
                }

                RainbowBattleManager.AddFlag(
                    Session,
                    rainbowTeam,
                    (RainbowNpcType)guriPacket.Argument,
                    (int)guriPacket.User);
            }

            if (!guriPacket.Data.HasValue && guriPacket.Type == (long)GuriType.Emoticon)
            {
                return;
            }

            character.Event.EmitEvent(new GuriEvent
            {
                Type = guriPacket.Type,
                Argument = guriPacket.Argument,
                Data = guriPacket.Data ?? 0,
                User = guriPacket.User,
                Value = guriPacket.Value
            });
        }

        private void SendDiagnostic(string message, byte type = 10) =>
            Session.SendPacket(Session.Character.GenerateSay(message, type));

        private static HandlerSort ParseSort(string mode)
        {
            switch (mode)
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
                case "missing":
                    return HandlerSort.Errors;
                default:
                    return HandlerSort.TotalTime;
            }
        }

        private static string ResolveTypeName(long type)
        {
            if (type < int.MinValue || type > int.MaxValue)
            {
                return "Unknown";
            }

            int numericType = (int)type;
            return Enum.IsDefined(typeof(GuriType), numericType)
                ? ((GuriType)numericType).ToString()
                : "Unknown";
        }

        #endregion
    }
}
