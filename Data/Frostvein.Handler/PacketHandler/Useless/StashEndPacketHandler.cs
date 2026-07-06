using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Useless
{
    public class StashEndPacketHandler : IPacketHandler
    {
        #region Instantiation

        public StashEndPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FStashEnd(StashEndPacket stashEndPacket)
        {
            // idk
        }

        #endregion
    }
}