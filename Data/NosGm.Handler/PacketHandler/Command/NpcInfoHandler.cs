using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
{
    public class NpcInfoHandler : IPacketHandler
    {
        #region Instantiation

        public NpcInfoHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void NpcInfo(NpcInfoPacket NpcInfoPacket)
        {
            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), "[NpcInfo]");
            MapNpc npc = Session.CurrentMapInstance.GetNpc(Session.Character.LastNpcMonsterId);
            if (npc != null)
            {
                int distance = Map.GetDistance(new MapCell
                {
                    X = Session.Character.PositionX,
                    Y = Session.Character.PositionY
                }, new MapCell
                {
                    X = npc.MapX,
                    Y = npc.MapY
                });
                if (!npc.IsMate && !npc.IsDisabled && !npc.IsProtected)
                {
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("NPC_INFORMATIONS"), npc.MapNpcId, npc.Npc.Name, npc.NpcVNum, npc.MapId, npc.MapX, npc.MapY), 12));
                }
            }
        }

        #endregion
    }
}
