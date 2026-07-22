using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class MvePacketHandler : IPacketHandler
    {
        #region Instantiation

        public MvePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MoveEquipment(MvePacket mvePacket)
        {
            if (mvePacket != null)
            {
                lock (Session.Character.Inventory)
                {
                    if (mvePacket.Slot.Equals(mvePacket.DestinationSlot) && mvePacket.InventoryType.Equals(mvePacket.DestinationInventoryType))
                    {
                        return;
                    }

                    if (mvePacket.DestinationSlot > 48 + (Session.Character.HaveBackpack() ? 1 : 0) * 12 + (Session.Character.HaveExtension() ? 1 : 0) * 60)
                    {
                        return;
                    }

                    if (Session.Character.InExchangeOrTrade)
                    {
                        return;
                    }

                    var sourceItem = Session.Character.Inventory.LoadBySlotAndType(mvePacket.Slot, mvePacket.InventoryType);

                    if (sourceItem == null)
                    {
                        return;
                    }

                    // Why would you want to "move" items from bazaar's inventory, buddy?
                    if (mvePacket.InventoryType.Equals(InventoryType.Bazaar))
                    {
                        return;
                    }

                    if (mvePacket.InventoryType == InventoryType.Bazaar || mvePacket.DestinationInventoryType == InventoryType.Miniland)
                    {
                        Logger.LogUserEvent("DUPE TRY", Session.Character.GenerateIdentity(), "CharacterId: " + Session.Character.CharacterId + " mvePacket");
                        return;
                    }

                    if (mvePacket.DestinationInventoryType == InventoryType.Bazaar || mvePacket.DestinationInventoryType == InventoryType.Etc || mvePacket.DestinationInventoryType == InventoryType.Main || mvePacket.DestinationInventoryType == InventoryType.Warehouse || mvePacket.DestinationInventoryType == InventoryType.Wear)
                    {
                        Session.SendPacket("msg 4 Action blocked");
                        return;
                    }

                    if (!mvePacket.InventoryType.Equals(mvePacket.DestinationInventoryType) && sourceItem.Item.ItemType != ItemType.Specialist && sourceItem.Item.ItemType != ItemType.Fashion)
                    {
                        return;
                    }

                    if (sourceItem.Item.ItemType == ItemType.Specialist && mvePacket.DestinationInventoryType != InventoryType.Specialist && mvePacket.DestinationInventoryType != InventoryType.Equipment)
                    {
                        return;
                    }

                    if (sourceItem.Item.ItemType == ItemType.Fashion && mvePacket.DestinationInventoryType != InventoryType.Costume && mvePacket.DestinationInventoryType != InventoryType.Equipment)
                    {
                        return;
                    }

                    if (mvePacket.DestinationInventoryType == InventoryType.Miniland)
                    {
                        Session.SendPacket("msg Stop trying to dupe.");
                        return;
                    }

                    var inv = Session.Character.Inventory.MoveInInventory(mvePacket.Slot, mvePacket.InventoryType, mvePacket.DestinationInventoryType, mvePacket.DestinationSlot, false);
                    if (inv != null)
                    {
                        Session.SendPacket(inv.GenerateInventoryAdd());
                        Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(mvePacket.InventoryType, mvePacket.Slot));
                    }
                }
            }
                
        }

        #endregion
    }
}