using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Inventory
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