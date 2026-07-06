using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class ScptcsPacketHandler : IPacketHandler
    {
        #region Instantiation

        public ScptcsPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void scpcts(ScptcsPacket packet)
        {
            // idk
        }

        #endregion
    }
}