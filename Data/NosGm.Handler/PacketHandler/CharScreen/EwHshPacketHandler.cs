using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.CharScreen
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
