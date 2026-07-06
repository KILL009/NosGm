using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class LbsPacketPacketHandler : IPacketHandler
    {
        #region Instantiation

        public LbsPacketPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Lbs(LbsPacket lbsPacket)
        {
            // idk
        }

        #endregion
    }
}