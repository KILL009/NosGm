using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Useless
{
    public class PdtClosePacketHandler : IPacketHandler
    {
        #region Instantiation

        public PdtClosePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void pdtclose(PdtClosePacket packet)
        {
            // idk
        }

        #endregion
    }
}