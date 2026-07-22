using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class DepositPacketHandler : IPacketHandler
    {
        #region Instantiation

        public DepositPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Deposit(DepositPacket depositPacket)
        {
            Session.SendPacket("info This Feature has been disabled for now");
            return;
        }

        private async Task AddToPartnerBackpackAsync(ItemInstance originalItem, DepositPacket packet, ItemInstance partnerItem)
        {
            Session.SendPacket("info This Feature has been disabled for now");
            return;
        }

        #endregion
    }
}