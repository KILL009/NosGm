using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Family
{
    public class FrankCtsPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FrankCtsPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyRank(FrankCtsPacket frankCtsPacket)
        {
            Session.SendPacket(Character.GenerateFrank(frankCtsPacket.Type, Session));
        }

        #endregion
    }
}