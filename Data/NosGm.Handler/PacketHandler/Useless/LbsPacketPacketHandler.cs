using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Useless
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