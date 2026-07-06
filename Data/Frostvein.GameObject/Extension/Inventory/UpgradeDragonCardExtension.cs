using System.Linq;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class UpgradeDragonCardExtension
    {
        #region Methods

        public static void UpgradeDragonCard(this ItemInstance e, ClientSession s, byte value)
        {
            if (e.Upgrade == 20)
            {
                return;
            }

            switch (e.Upgrade)
            {
                case 15:
                    #region Upgrade

                    var rnd = ServerManager.RandomNumber();
                    if (rnd < 1.2)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 80);
                        s.Character.Inventory.RemoveItemAmount(1030, 32);
                        s.Character.Inventory.RemoveItemAmount(2630, 2);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1250000;
                        s.SendPacket(s.Character.GenerateGold());
                        s.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, 3005), s.Character.PositionX, s.Character.PositionY);
                        s.SendPacket("msg 4 Upgrade successful");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade was successful.", 12));
                        e.Upgrade++;
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(e.GenerateInventoryAdd());
                    }
                    if (rnd > 1.2)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 80);
                        s.Character.Inventory.RemoveItemAmount(1030, 32);
                        s.Character.Inventory.RemoveItemAmount(2630, 2);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1250000;
                        s.SendPacket("msg 4 Upgrade failed");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade failed.", 11));
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(s.Character.GenerateGold());
                    }

                    #endregion 
                    break;

                case 16:
                    #region Upgrade

                    var rnd2 = ServerManager.RandomNumber();
                    if (rnd2 < 1)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 90);
                        s.Character.Inventory.RemoveItemAmount(1030, 34);
                        s.Character.Inventory.RemoveItemAmount(2630, 4);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1500000;
                        s.SendPacket(s.Character.GenerateGold());
                        s.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, 3005), s.Character.PositionX, s.Character.PositionY);
                        s.SendPacket("msg 4 Upgrade successful");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade was successful.", 12));
                        e.Upgrade++;
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(e.GenerateInventoryAdd());
                    }
                    if (rnd2 > 1)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 90);
                        s.Character.Inventory.RemoveItemAmount(1030, 34);
                        s.Character.Inventory.RemoveItemAmount(2630, 4);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1500000;
                        s.SendPacket("msg 4 Upgrade failed");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade failed.", 11));
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(s.Character.GenerateGold());
                    }

                    #endregion 
                    break;

                case 17:
                    #region Upgrade

                    var rnd3 = ServerManager.RandomNumber();
                    if (rnd3 < 0.8)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 100);
                        s.Character.Inventory.RemoveItemAmount(1030, 36);
                        s.Character.Inventory.RemoveItemAmount(2630, 6);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1750000;
                        s.SendPacket(s.Character.GenerateGold());
                        s.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, 3005), s.Character.PositionX, s.Character.PositionY);
                        s.SendPacket("msg 4 Upgrade successful");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade was successful.", 12));
                        e.Upgrade++;
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(e.GenerateInventoryAdd());
                    }
                    if (rnd3 > 0.8)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 100);
                        s.Character.Inventory.RemoveItemAmount(1030, 36);
                        s.Character.Inventory.RemoveItemAmount(2630, 6);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 1750000;
                        s.SendPacket("msg 4 Upgrade failed");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade failed.", 11));
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(s.Character.GenerateGold());
                    }

                    #endregion 
                    break;

                case 18:
                    #region Upgrade

                    var rnd4 = ServerManager.RandomNumber();
                    if (rnd4 < 0.6)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 110);
                        s.Character.Inventory.RemoveItemAmount(1030, 38);
                        s.Character.Inventory.RemoveItemAmount(2630, 8);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 2000000;
                        s.SendPacket(s.Character.GenerateGold());
                        s.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, 3005), s.Character.PositionX, s.Character.PositionY);
                        s.SendPacket("msg 4 Upgrade successful");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade was successful.", 12));
                        e.Upgrade++;
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(e.GenerateInventoryAdd());
                    }
                    if (rnd4 > 0.6)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 110);
                        s.Character.Inventory.RemoveItemAmount(1030, 38);
                        s.Character.Inventory.RemoveItemAmount(2630, 8);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 2000000;
                        s.SendPacket("msg 4 Upgrade failed");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade failed.", 11));
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(s.Character.GenerateGold());
                    }

                    #endregion 
                    break;

                case 19:
                    #region Upgrade

                    var rnd5 = ServerManager.RandomNumber();
                    if (rnd5 < 0.4)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 120);
                        s.Character.Inventory.RemoveItemAmount(1030, 40);
                        s.Character.Inventory.RemoveItemAmount(2630, 10);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 2250000;
                        s.SendPacket(s.Character.GenerateGold());
                        s.CurrentMapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, 3005), s.Character.PositionX, s.Character.PositionY);
                        s.SendPacket("msg 4 Upgrade successful");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade was successful.", 12));
                        e.Upgrade++;
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(e.GenerateInventoryAdd());
                    }
                    if (rnd5 > 0.4)
                    {
                        s.Character.Inventory.RemoveItemAmount(2282, 120);
                        s.Character.Inventory.RemoveItemAmount(1030, 40);
                        s.Character.Inventory.RemoveItemAmount(2630, 10);
                        s.Character.Inventory.RemoveItemAmount(9287, 1);
                        s.Character.Gold -= 2250000;
                        s.SendPacket("msg 4 Upgrade failed");
                        s.SendPacket(s.Character.GenerateSay("The Specialist Upgrade failed.", 11));
                        s.SendShopEnd();
                        s.SendPacket(s.Character.GenerateEq());
                        s.SendPacket(s.Character.GenerateGold());
                    }

                    #endregion 
                    break;
            }

        }
        #endregion
    }
}