using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.CharScreen
{
    internal class EwHshPacketHandler : IPacketHandler
    {
        #region Instantiation

        public EwHshPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task EwHshAsync(EwHshPacket ewHshPacket)
        {
            if (ewHshPacket.PacketData != null)
            {
               
            }
            else if (ewHshPacket.PacketData == null)
            {
                Session?.Disconnect();
                return;
            }
        }
        #endregion
    }
}
