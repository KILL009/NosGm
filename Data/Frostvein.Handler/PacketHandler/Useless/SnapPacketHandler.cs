using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class SnapPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SnapPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Snap(SnapPacket snapPacket)
        {
            // Not needed for now. (pictures)
        }

        #endregion
    }
}