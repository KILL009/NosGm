using System.Linq;
using Frostvein.Core;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class CraftFairyService
    {
        #region Methods

        public static void CraftZenas(this ItemInstance e, ClientSession s, byte value)
        {
            int twilightEssence = 2434;

            if (s.Character.Inventory.CountItem(5875) < 1)
            {
                return;
            }

            if (s.Character.Inventory.CountItem(twilightEssence) < 3)
            {
                s.SendPacket("msg 4 You dont have enough Twilight Essence");
                return;
            }

            if (s.Character.Gold < 2000000)
            {
                s.SendPacket("msg 4 You dont have enough Gold");
                return;
            }

            var rnd = ServerManager.RandomNumber();
            if (rnd < 10)
            {
                var newItem = e;
                newItem.ItemVNum = (short)(4705 + (newItem.Item.Element - 1));
                s.Character.Inventory.RemoveItemFromInventory(e.Id);
                s.Character.Inventory.RemoveItemAmount(twilightEssence, 3);
                s.Character.Inventory.RemoveItemAmount(5875, 1);
                s.Character.Gold -= 2000000;
                s.SendPacket(s.Character.GenerateGold());
                s.Character.GiftAdd((short)(4705 + newItem.Item.Element - 1), 1);
                s.SendPacket($"pdti 11 {newItem.ItemVNum} 1 29 0 0");
                s.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, s.Character.CharacterId, 1324));
                s.SendPacket("msg 4 Crafting successful!");
                s.SendShopEnd();
            }
            if (rnd > 10)
            {
                s.SendPacket("msg 4 Crafting failed.");
                s.Character.Inventory.RemoveItemAmount(twilightEssence, 3);
                s.Character.Inventory.RemoveItemAmount(5875, 1);
                s.Character.Gold -= 2000000;
                s.SendPacket(s.Character.GenerateGold());
                s.SendShopEnd();
            }

        }

        public static void CraftErenia(this ItemInstance e, ClientSession s, byte value)
        {
            int abyssalEssence = 2435;

            if (s.Character.Inventory.CountItem(5876) < 1)
            {
                return;
            }

            if (s.Character.Inventory.CountItem(abyssalEssence) < 3)
            {
                s.SendPacket("msg 4 You dont have enough Abyssal Essence");
                return;
            }

            if (s.Character.Gold < 2000000)
            {
                s.SendPacket("msg 4 You dont have enough Gold");
                return;
            }

            var rnd = ServerManager.RandomNumber();
            if (rnd < 10)
            {
                var newItem = e;
                newItem.ItemVNum = (short)(4709 + (newItem.Item.Element - 1));
                s.Character.Inventory.RemoveItemFromInventory(e.Id);
                s.Character.Inventory.RemoveItemAmount(abyssalEssence, 3);
                s.Character.Inventory.RemoveItemAmount(5876, 1);
                s.Character.Gold -= 2000000;
                s.SendPacket(s.Character.GenerateGold());
                s.Character.GiftAdd((short)(4709 + newItem.Item.Element - 1), 1);
                s.SendPacket($"pdti 11 {newItem.ItemVNum} 1 29 0 0");
                s.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, s.Character.CharacterId, 1324));
                s.SendPacket("msg 4 Crafting successful!");
                s.SendShopEnd();
            }

            if (rnd > 10)
            {
                s.SendPacket("msg 4 Crafting failed.");
                s.Character.Inventory.RemoveItemAmount(abyssalEssence, 3);
                s.Character.Inventory.RemoveItemAmount(5876, 1);
                s.Character.Gold -= 2000000;
                s.SendPacket(s.Character.GenerateGold());
                s.SendShopEnd();
            }
        }

        public static void CraftFernon(this ItemInstance e, ClientSession s, byte value)
        {
            int fairyKingEssence = 2444;

            if (s.Character.Inventory.CountItem(5877) < 1)
            {
                return;
            }

            if (s.Character.Inventory.CountItem(fairyKingEssence) < 1)
            {
                s.SendPacket("msg 4 You dont have enough Fairy King's Essence");
                return;
            }

            if (s.Character.Gold < 50000000)
            {
                s.SendPacket("msg 4 You dont have enough Gold");
                return;
            }

            var newItem = e;
            newItem.ItemVNum = (short)(4713 + (newItem.Item.Element - 1));
            s.Character.Inventory.RemoveItemFromInventory(e.Id);
            s.Character.Inventory.RemoveItemAmount(fairyKingEssence, 1);
            s.Character.Inventory.RemoveItemAmount(5877, 1);
            s.Character.Gold -= 50000000;
            s.SendPacket(s.Character.GenerateGold());
            s.Character.GiftAdd((short)(4713 + newItem.Item.Element - 1), 1);
            s.SendPacket($"pdti 11 {newItem.ItemVNum} 1 29 0 0");
            s.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, s.Character.CharacterId, 1324));
            s.SendShopEnd();
        }

        #endregion
    }
}