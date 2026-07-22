using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    public class PromoteHandler : IPacketHandler
    {
        #region Instantiation

        public PromoteHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Promote(PromotePacket promotePacket)
        {
          
        }

        #endregion
    }
}