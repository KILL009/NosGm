using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static System.Collections.Specialized.BitVector32;

namespace Frostvein.GameObject.Extension
{
    public static class UpgradeableExtension
    {
        public static async Task Upgrade(ClientSession session, ItemInstance inv, UpgradeableType upgradeableType, string[] packetsplit = null)
        {
            switch (upgradeableType)
            {
                case UpgradeableType.Fairy:
                    int rnd = ServerManager.RandomNumber(0, 100);
                    if (byte.TryParse(packetsplit[9], out var islot))
                    {
                        var wearInstance = session.Character.Inventory.LoadBySlotAndType(islot, InventoryType.Equipment);
                        if (rnd > 10)
                        {
                            switch (wearInstance.Item.VNum)
                            {
                                case 4129:
                                    session.Character.Inventory.RemoveItemAmount(4129, 1);
                                    session.Character.GiftAdd(11003, 1);
                                    session.SendPacket("msg 4 The Upgrade succeeded");
                                    break;

                                case 4130:
                                    session.Character.Inventory.RemoveItemAmount(4129, 1);
                                    session.Character.GiftAdd(11004, 1);
                                    session.SendPacket("msg 4 The Upgrade succeeded");
                                    break;

                                case 4131:
                                    session.Character.Inventory.RemoveItemAmount(4129, 1);
                                    session.Character.GiftAdd(11005, 1);
                                    session.SendPacket("msg 4 The Upgrade succeeded");
                                    break;

                                case 4132:
                                    session.Character.Inventory.RemoveItemAmount(4129, 1);
                                    session.Character.GiftAdd(11006, 1);
                                    session.SendPacket("msg 4 The Upgrade succeeded");
                                    break;
                            }
                            session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                            session.SendPacket("msg 4 The Upgrade failed");
                            return;
                        }
                        //TODO COUNT
                        session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
                    }
                    break;
            }
        }
    }
}