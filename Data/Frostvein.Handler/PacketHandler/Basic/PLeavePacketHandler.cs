using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class PLeavePacketHandler : IPacketHandler
    {
        #region Instantiation

        public PLeavePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task GroupLeave(PLeavePacket pleavePacket)
        {
            ServerManager.Instance.GroupLeave(Session);
        }

        #endregion
    }
}