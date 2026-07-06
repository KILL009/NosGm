using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class EquipmentInfoPacketHandler : IPacketHandler
    {
        #region Instantiation

        public EquipmentInfoPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void EquipmentInfo(EquipmentInfoPacket equipmentInfoPacket)
        {
            if (equipmentInfoPacket != null)
            {
                var isNpcShopItem = false;
                ItemInstance inventory = null;
                switch (equipmentInfoPacket.Type)
                {
                    case 0:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,InventoryType.Wear);
                        break;

                    case 1:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,InventoryType.Equipment);
                        break;

                    case 2:
                        isNpcShopItem = true;
                        if (ServerManager.GetItem(equipmentInfoPacket.Slot) != null)
                        {
                            inventory = new ItemInstance(equipmentInfoPacket.Slot, 1);
                            break;
                        }

                        return;

                    case 5:
                        if (Session.Character.ExchangeInfo != null)
                        {
                            var sess = ServerManager.Instance.GetSessionByCharacterId(Session.Character.ExchangeInfo.TargetCharacterId);
                            if (sess?.Character.ExchangeInfo?.ExchangeList?.ElementAtOrDefault(equipmentInfoPacket.Slot) != null)
                            {
                                var id = sess.Character.ExchangeInfo.ExchangeList[equipmentInfoPacket.Slot].Id;

                                inventory = sess.Character.Inventory.GetItemInstanceById(id);
                            }
                        }

                        break;

                    case 6:
                        if (equipmentInfoPacket.ShopOwnerId != null)
                        {
                            var shop = Session.CurrentMapInstance.UserShops.FirstOrDefault(mapshop => mapshop.Value.OwnerId.Equals(equipmentInfoPacket.ShopOwnerId));
                            var item = shop.Value?.Items.Find(i => i.ShopSlot.Equals(equipmentInfoPacket.Slot));
                            if (item != null)
                            {
                                inventory = item.ItemInstance;
                            }
                        }

                        break;

                    case 7:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.MateSlot,(InventoryType)(12 + equipmentInfoPacket.Slot));
                        break;

                    case 10:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,InventoryType.Specialist);
                        break;

                    case 11:
                        inventory = Session.Character.Inventory.LoadBySlotAndType(equipmentInfoPacket.Slot,InventoryType.Costume);
                        break;
                }

                if (inventory?.Item != null)
                {
                    if (inventory.IsEmpty || isNpcShopItem)
                    {
                        Session.SendPacket(inventory.GenerateEInfo());
                        return;
                    }

                     Session.SendPacket(inventory.Item.EquipmentSlot != EquipmentType.Sp ? inventory.GenerateEInfo() :
                        inventory.Item.SpType == 0 && inventory.Item.ItemSubType == 4 ? inventory.GeneratePslInfo(inventory) :
                        inventory.GenerateSlInfo(Session));
                }
            }
        }

        #endregion
    }
}