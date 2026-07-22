using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Family
{
    public class GListPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GListPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyList(GListPacket gListPacket)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null && gListPacket.Type == 2)
            {
                Session.SendPacket(Session.Character.GenerateGInfo());
                Session.SendPacket(Session.Character.GenerateFamilyMember());
                Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
            }
        }

        #endregion
    }
}