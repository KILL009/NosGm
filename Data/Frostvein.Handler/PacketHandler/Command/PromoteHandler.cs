using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
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