using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class ShopClosePacketHandler : IPacketHandler
    {
        #region Instantiation

        public ShopClosePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ShopClose(ShopClosePacket shopClosePacket)
        {
            // Not needed for now.
        }

        #endregion
    }
}