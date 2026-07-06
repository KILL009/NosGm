using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class CClosePacketHandler : IPacketHandler
    {
        #region Instantiation

        public CClosePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void CClose(CClosePacket cClosePacket)
        {
            // idk
        }

        #endregion
    }
}