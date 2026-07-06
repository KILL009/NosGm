using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;

namespace Frostvein.Handler.PacketHandler.Family
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