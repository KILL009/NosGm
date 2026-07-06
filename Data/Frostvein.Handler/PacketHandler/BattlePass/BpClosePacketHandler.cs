using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
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