
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Extension.Inventory;
using Frostvein.GameObject.Extension.Item;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;


namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class UpgradePacketHandler : IPacketHandler
    {
        #region Instantiation

        public UpgradePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Upgrade(UpgradePacket upgradePacket)
        {
            if (upgradePacket.UpgradeType == 0 && upgradePacket.InventoryType == 0 && upgradePacket.Slot == 0)
            {
                Session.SendPacket("msg 4 Action blocked");
                //LOGGER($"[EXPLOIT] {Session.Character.Name} tried to use a UpgradePacket Exploit. | Data: up_gr 0 0 0");
                return;
            }

            if (upgradePacket.UpgradeType == 0)
            {
                Session.SendPacket("msg 4 Action blocked");
                //LOGGER($"[EXPLOIT] {Session.Character.Name} tried to use a UpgradePacket Exploit. | Data: up_gr 0 0 0");
                return;
            }

            if (upgradePacket == null || Session.Character.ExchangeInfo?.ExchangeList.Count > 0
                || Session.Character.Speed == 0 || Session.Character.LastDelay.AddSeconds(5) > DateTime.Now)
            {
                return;
            }

            InventoryType inventoryType = upgradePacket.InventoryType;
            short uptype = upgradePacket.UpgradeType, slot = upgradePacket.Slot;
            Session.Character.LastDelay = DateTime.Now;
            ItemInstance inventory;
            ItemInstance specialist2 = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
            switch (uptype)
            {
                case 0:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.ConvertToPartnerEquipment(Session);
                        }
                    }
                    break;

                case 1:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Normal, UpgradeProtection.None);
                        }
                    }
                    break;

                case 3:

                    //up_gr 3 0 0 7 1 1 20 99
                    string[] originalSplit = upgradePacket.OriginalContent.Split(' ');
                    if (originalSplit.Length == 10
                        && byte.TryParse(originalSplit[5], out byte firstSlot)
                        && byte.TryParse(originalSplit[8], out byte secondSlot))
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(firstSlot, InventoryType.Equipment);
                        if (inventory != null
                            && (inventory.Item.EquipmentSlot == EquipmentType.Necklace
                             || inventory.Item.EquipmentSlot == EquipmentType.Bracelet
                             || inventory.Item.EquipmentSlot == EquipmentType.Ring)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            ItemInstance cellon =
                                Session.Character.Inventory.LoadBySlotAndType(secondSlot,
                                    InventoryType.Main);
                            if (cellon?.ItemVNum > 1016 && cellon.ItemVNum < 1027)
                            {
                                inventory.OptionItem(Session, cellon.ItemVNum);
                            }
                        }
                    }
                    break;

                case 7:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if (inventory.Item.EquipmentSlot == EquipmentType.Armor || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                        {
                            RarifyMode mode = RarifyMode.Normal;
                            RarifyProtection protection = RarifyProtection.None;
                            ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((short)EquipmentType.Amulet, InventoryType.Wear);
                            if (amulet != null)
                            {
                                switch (amulet.Item.Effect)
                                {
                                    case 791:
                                        protection = RarifyProtection.RedAmulet;
                                        break;
                                    case 792:
                                        protection = RarifyProtection.BlueAmulet;
                                        break;
                                    case 794:
                                        protection = RarifyProtection.HeroicAmulet;
                                        break;
                                    case 795:
                                        protection = RarifyProtection.RandomHeroicAmulet;
                                        break;
                                    case 796:
                                        if (inventory.Item.IsHeroic)
                                        {
                                            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateEff(3006));
                                            Session.Character.DeleteItemByItemInstanceId(amulet.Id);
                                            Session.SendPacket("info Your Item increased its Rarity Level by 1");
                                            mode = RarifyMode.Success;
                                        }
                                        break;
                                }
                            }
                            inventory.RarifyItem(Session, mode, protection);
                        }

                        Session.SendPacket("shop_end 1");
                    }
                    break;

                case 8:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (upgradePacket.InventoryType2 != null && upgradePacket.Slot2 != null)
                    {
                        ItemInstance inventory2 =
                            Session.Character.Inventory.LoadBySlotAndType((byte)upgradePacket.Slot2,
                                (InventoryType)upgradePacket.InventoryType2);

                        if (inventory != null && inventory2 != null && !Equals(inventory, inventory2))
                        {
                            inventory.Sum(Session, inventory2);
                        }
                    }
                    break;

                case 9:
                    ItemInstance specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                specialist.UpgradeSp(Session, UpgradeProtection.None);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 20:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Normal, UpgradeProtection.Protected);
                        }
                    }
                    break;

                case 21:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.RarifyItem(Session, RarifyMode.Normal, RarifyProtection.Scroll);
                        }
                    }
                    break;

                case 23:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        Session.SendPacket("info Cool shit would happen");
                    }
                    break;

                case 25:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Upgrade > 9)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("MUST_USE_ITEM"), ServerManager.GetItem(1364).Name), 0));
                                return;
                            }
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                specialist.UpgradeSp(Session, UpgradeProtection.Protected);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 26:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Upgrade <= 9)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("MUST_USE_ITEM"), ServerManager.GetItem(1363).Name), 0));
                                return;
                            }
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                specialist.UpgradeSp(Session, UpgradeProtection.Protected);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 35:
                    // Event Chicken Upgrade Scroll SP
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist?.ItemVNum == 907)
                    {
                        if (specialist2 != null)
                        {
                            if (specialist2.Rare != -2)
                            {
                                if (specialist2.Item.EquipmentSlot == EquipmentType.Sp)
                                {
                                    specialist.UpgradeSpFun(Session, UpgradeProtection.Event, 1);
                                }
                            }
                            else
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                            }
                        }
                    }
                    break;

                case 38: // Event Pyjama Upgrade SP
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist?.ItemVNum == 900)
                    {
                        if (specialist2 != null)
                        {
                            if (specialist2.Rare != -2)
                            {
                                if (specialist2.Item.EquipmentSlot == EquipmentType.Sp)
                                {
                                    specialist.UpgradeSpFun(Session, UpgradeProtection.Event, 2);
                                }
                            }
                            else
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                            }
                        }
                    }
                    break;

                case 41:
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist != null)
                    {
                        if (specialist.Rare != -2)
                        {
                            if (specialist.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                specialist.PerfectSP(Session);
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                        }
                    }
                    break;

                case 42:
                    // Event Pirat Upgrade SP
                    specialist = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (specialist?.ItemVNum == 4099)
                    {
                        if (specialist2 != null)
                        {
                            if (specialist2.Rare != -2)
                            {
                                if (specialist2.Item.EquipmentSlot == EquipmentType.Sp)
                                {
                                    specialist.UpgradeSpFun(Session, UpgradeProtection.Event, 3);
                                }
                            }
                            else
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_UPGRADE_DESTROYED_SP"), 0));
                            }
                        }
                    }
                    break;

                case 43:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (inventory != null)
                    {
                        if ((inventory.Item.EquipmentSlot == EquipmentType.Armor
                             || inventory.Item.EquipmentSlot == EquipmentType.MainWeapon
                             || inventory.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                            && inventory.Item.ItemType != ItemType.Shell && inventory.Item.Type == InventoryType.Equipment)
                        {
                            inventory.UpgradeItem(Session, UpgradeMode.Reduced, UpgradeProtection.Protected);
                        }
                    }
                    break;

                case 53:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    var secondItem = Session.Character.Inventory.LoadBySlotAndType((byte)slot, inventoryType);
                    if (inventory == null)
                    {
                        return;
                    }

                    if (secondItem == null)
                    {
                        return;
                    }

                    inventory.FusionItem(Session, secondItem);
                    break;

                // Craft tattoo
                case 81:
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Main);

                        if (inventory == null) return;

                        inventory.CraftTattoo(Session);
                    }
                    break;

                // Rune Upgrade / Remove
                case 83:
                case 84: // scroll premium
                case 86: // scroll basic
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType((byte)upgradePacket.InventoryType2,
                            InventoryType.Equipment);

                        if (inventory == null) return;

                        switch (inventoryType)
                        {
                            // Remove Rune
                            case (InventoryType)2:
                                inventory.RemoveRune(Session);
                                break;

                            // Upgrade Rune
                            case (InventoryType)1:
                            case (InventoryType)3: // basic
                            case (InventoryType)4: // Premium
                                inventory.UpgradeRune(Session,
                                    uptype == 84 ? UpgradeRuneType.Premium :
                                    uptype == 86 ? UpgradeRuneType.Basic : UpgradeRuneType.None);
                                break;

                            default:
                                return;
                        }
                    }
                    break;

                case 50: //Zenas
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);

                        if (inventory == null) return;

                        inventory.CraftZenas(Session, (byte)(uptype - 50));
                    }
                    break;

                case 51: //Erenia
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);

                        if (inventory == null) return;

                        inventory.CraftErenia(Session, (byte)(uptype - 50));
                    }
                    break;

                case 52: //Fernon
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);

                        if (inventory == null) return;

                        inventory.CraftFernon(Session, (byte)(uptype - 50));
                    }
                    break;

                // Tattoo Upgrade / Remove
                case 82:
                case 85:
                    {
                        var ski = Session.Character.Skills.FirstOrDefault(s => s.SkillVNum == slot);

                        if (ski == null) return;

                        switch (inventoryType)
                        {
                            // Remove Tattoo Inked
                            case (InventoryType)2:
                                if (uptype != 82) return;
                                ski.RemoveTattoo(Session);
                                break;

                            // Upgrade Tattoo
                            case (InventoryType)1: // NPC
                            case (InventoryType)3: // with scroll
                                ski.UpgradeTattoo(Session, uptype == 82 ? false : true);
                                break;

                            default:
                                return;
                        }
                    }
                    break;

                //Dragon Specialist Scroll
                case 90:
                    {
                        inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);

                        if (inventory == null) return;

                        inventory.UpgradeDragonCard(Session, (byte)(uptype - 50));
                    }
                    break;


                //Partner Fusion
                case 93:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, inventoryType);
                    if (upgradePacket.InventoryType2 != null && upgradePacket.Slot2 != null)
                    {
                        ItemInstance inventory2 =
                            Session.Character.Inventory.LoadBySlotAndType((byte)upgradePacket.Slot2,
                                (InventoryType)upgradePacket.InventoryType2);

                        if (inventory != null && inventory2 != null && !Equals(inventory, inventory2))
                        {
                            inventory.PSPFusion(Session, inventory2);
                        }
                    }
                    break;

                case 95:
                    inventory = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);

                    if (inventory == null) return;

                    inventory.UpgradeFairy();
                    break;

            }
            #endregion
        }
    }
}