using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class BpClosePacketHandler : IPacketHandler
    {
        #region Instantiation

        public BpClosePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task BpCloseAsync(BpClosePacket bpClosePacket)
        {
          
        }

        #endregion
    }
}