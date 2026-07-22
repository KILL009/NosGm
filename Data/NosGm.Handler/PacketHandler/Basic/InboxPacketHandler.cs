using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class InboxPacketHandler : IPacketHandler
    {
        #region Instantiation

        public InboxPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Inbox(InboxPacket inboxPacket)
        {
            if (Session?.CurrentMapInstance != null)
            {
                if (inboxPacket.Amount != 0 && inboxPacket.Data != null)
                {
                    string Answer = inboxPacket.Data;

                    Session.SendPacket(UserInterfaceHelper.GenerateInbox(Answer));
                }
            }
        }

        #endregion
    }
}
