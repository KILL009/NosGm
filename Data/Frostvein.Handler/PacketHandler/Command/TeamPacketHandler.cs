using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class TeamPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TeamPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

    }
}
