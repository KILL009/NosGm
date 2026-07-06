
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class MagicianExtension
    {
        public static async Task UpgradeFairy(ClientSession Session)
        {
            ItemInstance Fairy = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (Fairy == null) { return; }

            if (Fairy.ItemVNum >= 4705 && Fairy.ItemVNum <= 4712)
            {
                if (Fairy.FairyLevel == 120)
                {
                    int rnd = ServerManager.RandomNumber(0, 100);
                    if (rnd < 5)
                    {
                        switch (Fairy.ItemVNum)
                        {
                            case 4705:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4713, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Fire)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4706:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4714, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Water)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4707:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4715, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Light)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4708:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4716, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Shadow)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4709:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4713, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Fire)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4710:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4714, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Water)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4711:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4715, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Light)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                            case 4712:
                                Session.Character.Inventory.RemoveItemFromInventory(Fairy.Id, Fairy.ItemVNum);
                                Session.SendPacket(Session.Character.GenerateEq());
                                Session.SendPacket(Session.Character.GenerateEquipment());
                                Session.Character.GiftAdd(4716, 1);
                                MessageExtension.SendBubble(Session, "The Heaven Sent granted you your wish. You received a Fernon (Shadow)");
                                //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Successfully upgraded", LogType.UpgradeFairy);
                                Session.Character.Gold -= 10000000;
                                Session.SendPacket(Session.Character.GenerateGold());
                                break;

                        }
                    }
                    MessageExtension.SendGrey(Session, "Sadly, your wish wasnt granted");
                    Session.Character.Gold -= 10000000;
                    Session.SendPacket(Session.Character.GenerateGold());
                    //LOGGER(Session.Character.CharacterId, $"{Session.Character.Name}", "Not upgraded", LogType.UpgradeFairy);
                }
                else
                {
                    MessageExtension.SendRed(Session, "Your Fairy didnt reach the highest Level yet!");
                    MessageExtension.SendRed(Session, "Fairy Level required: 120");
                }
            }
            else
            {
                MessageExtension.SendRed(Session, "This isn't a Zenas or Erenia Fairy");
            }
        }
    }
}
