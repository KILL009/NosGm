using NosGm.Extension.Extension.Packet;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Npc
{
    public class SpptPacketHandler : IPacketHandler
    {

        public SpptPacketHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void FusionPspNpc(SpptPacket spptPacket)
        {
            ItemInstance itemToUpgrade = Session.Character.Inventory.LoadBySlotAndType(spptPacket.ItemToUpgrade, InventoryType.Equipment);
            short slotToFuse = spptPacket.ItemToFuse ?? -1;
            int nextUpgrade = 1 + itemToUpgrade.PartnerUpgradeLevel;
            if (itemToUpgrade != null)
            {
                if (itemToUpgrade.PartnerUpgradeLevel >= 100) return;

                if (slotToFuse != -1)
                {
                    ItemInstance itemToFuse = Session.Character.Inventory.LoadBySlotAndType(slotToFuse, InventoryType.Equipment);
                    byte upgradeValue = (byte)(itemToUpgrade.Item.Morph == itemToFuse.Item.Morph ? 4 : 2);

                    nextUpgrade = ItemHelper.FusionPspNextUpgrade(upgradeValue, itemToUpgrade.PartnerUpgradeLevel);

                    Session.SendPacket($"ptsp_data 0 {itemToUpgrade.PartnerUpgradeLevel} 0 {nextUpgrade} 8");
                }
                else
                    Session.SendPacket($"ptsp_data 0 {itemToUpgrade.PartnerUpgradeLevel} 0 -1 -1");

                int[] goldprice = { 100000, 1000000, 300000, 3000000, 600000, 6000000, 900000, 9000000, 1200000, 12000000 };
                byte[] itemAmount = { 10, 1, 15, 2, 20, 3, 25, 4, 30, 5 };
                short[] itemVNum = { 2283, 2284, 2285, 2511, 2512 };

                int currentGold = 0;
                short currentItem = 0;
                byte currentAmount = 0;
                short elementItem = 0;
                switch (itemToUpgrade.Item.Element)
                {
                    case 1:
                        elementItem = (short)(nextUpgrade < 60 ? 2514 : 2518);
                        break;
                    case 2:
                        elementItem = (short)(nextUpgrade < 60 ? 2515 : 2519);
                        break;
                    case 3:
                        elementItem = (short)(nextUpgrade < 60 ? 2517 : 2521);
                        break;
                    case 4:
                        elementItem = (short)(nextUpgrade < 60 ? 2516 : 2520);
                        break;
                }

                if (nextUpgrade <= 19)
                {
                    currentGold = goldprice[0];
                    currentAmount = itemAmount[0];
                    currentItem = itemVNum[0];
                }
                else if (nextUpgrade == 20)
                {
                    currentGold = goldprice[1];
                    currentAmount = itemAmount[1];
                    currentItem = elementItem;
                }
                else if (nextUpgrade > 20 && nextUpgrade < 40)
                {
                    currentGold = goldprice[2];
                    currentAmount = itemAmount[2];
                    currentItem = itemVNum[1];
                }
                else if (nextUpgrade == 40)
                {
                    currentGold = goldprice[3];
                    currentAmount = itemAmount[3];
                    currentItem = elementItem;
                }
                else if (nextUpgrade > 40 && nextUpgrade < 60)
                {
                    currentGold = goldprice[4];
                    currentAmount = itemAmount[4];
                    currentItem = itemVNum[2];
                }
                else if (nextUpgrade == 60)
                {
                    currentGold = goldprice[5];
                    currentAmount = itemAmount[5];
                    currentItem = elementItem;
                }
                else if (nextUpgrade > 60 && nextUpgrade < 80)
                {
                    currentGold = goldprice[6];
                    currentAmount = itemAmount[6];
                    currentItem = itemVNum[3];
                }
                else if (nextUpgrade == 80)
                {
                    currentGold = goldprice[7];
                    currentAmount = itemAmount[7];
                    currentItem = elementItem;
                }
                else if (nextUpgrade > 80 && nextUpgrade < 100)
                {
                    currentGold = goldprice[8];
                    currentAmount = itemAmount[8];
                    currentItem = itemVNum[4];
                }
                else if (nextUpgrade == 100)
                {
                    currentGold = goldprice[9];
                    currentAmount = itemAmount[9];
                    currentItem = elementItem;
                }

                Session.SendPacket($"ptsp_data 1 {currentGold} {currentItem} {currentAmount} 0");
            }
        }
    }
}