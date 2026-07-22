using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
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
