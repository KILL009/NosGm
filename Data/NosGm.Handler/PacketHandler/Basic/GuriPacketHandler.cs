using NosGm.Core;
using NosGm.Core.Diagnostics;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;
using NosGm.Packets.Packets.ClientPackets;
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

        #endregion
    }
}
