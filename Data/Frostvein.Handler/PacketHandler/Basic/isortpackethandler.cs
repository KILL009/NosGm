using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.Handler.Packets.basic
{
    public class ISortPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession Session;

        #endregion

        #region Instantiation

        public ISortPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Methods

        public void Sort(isortPacket e)
        {
            if (!CheckInvType(e.Type))
            {
                SendErrorMsgAsync(Session);
                return;
            }

            // Like Official server 
            var time = Session.Character.LastISort.AddSeconds(5);
            if (DateTime.Now <= time)
            {
                SendErrorMsgAsync(Session);
                return;
            }

            SortInv(Session, e.Type);
        }

        private bool CheckInvType(InventoryType type)
        {
            var AllowedEnum = new List<InventoryType>
            {
                InventoryType.Equipment,
                InventoryType.Main,
                InventoryType.Etc,
                InventoryType.Specialist,
                InventoryType.Costume,
                InventoryType.Main,
                InventoryType.Warehouse,
                InventoryType.PetWarehouse
            };

            if (!AllowedEnum.Contains(type))
            {
                return false;
            }

            return true;
        }

        private void SendErrorMsgAsync(ClientSession e)
        {
            e.SendPacket("msgi 3 1808 0 0 0 0 0");
        }

        private void SortInv(ClientSession e, InventoryType type)
        {
            e.Character.LastISort = DateTime.Now;
            e.Character.Inventory.Reorder(e, type);
        }

        #endregion
    }
}