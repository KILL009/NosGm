using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;
using NosGm.GameObject.Plugin.Event;

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
            else if (guriPacket.Type == 720)
            {
                MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == guriPacket.User);

                if (Session == null || Session?.Character?.MapInstance == null)
                {
                    return;
                }

                if (Session.Character.isFreezed == true)
                {
                    return;
                }

                //Packet Hacking
                if (npc == null)
                {
                    return;
                }


                int dist = Map.GetDistance(
                    new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY },
                    new MapCell { X = npc.MapX, Y = npc.MapY });
                if (dist > 5)
                {
                    return;
                }

                var RainbowTeam = ServerManager.Instance.RainbowBattleMembers.Find(s => s.Session.Contains(Session));

                if (RainbowTeam == null || RainbowBattleManager.AlreadyHaveFlag(RainbowTeam, (RainbowNpcType)guriPacket.Argument, (int)guriPacket.User))
                {
                    return;
                }

                RainbowBattleManager.AddFlag(Session, RainbowTeam, (RainbowNpcType)guriPacket.Argument, (int)guriPacket.User);
            }

            if (!guriPacket.Data.HasValue && guriPacket.Type == 10)
            {
                return;
            }
            var packetsplit = guriPacket.OriginalContent.Split(' ', '^');

            Session.Character.Event.EmitEvent(new GuriEvent
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