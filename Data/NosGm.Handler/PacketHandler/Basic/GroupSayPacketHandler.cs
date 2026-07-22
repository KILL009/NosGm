using NosGm.Packets.Packets.ClientPackets;
using NosGm.Packets.Packets.ServerPackets;

using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;


namespace NosGm.Handler.PacketHandler.Basic
{
    public class GroupSayPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GroupSayPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void GroupTalk(GroupSayPacket groupSayPacket)
        {
            if (!string.IsNullOrEmpty(groupSayPacket.Message))
            {
                ServerManager.Instance.Broadcast(Session, Session.Character.GenerateSpk(groupSayPacket.Message, 3), ReceiverType.Group);
                //LOGGER($"[Group][{Session.Character.Name}]: {groupSayPacket.Message}");
            }
        }

        #endregion
    }
}