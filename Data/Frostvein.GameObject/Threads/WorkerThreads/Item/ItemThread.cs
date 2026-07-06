using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using Frostvein.GameObeject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Extension;
using Frostvein.Data;
using Frostvein.Master.Library.Client;

namespace Frostvein.GameObject.ItemThread
{
    public static class ItemThread
    {

        public static void Add(ClientSession Session, short itemVNum, short amount, byte rare = 0, byte upgrade = 0, short design = 0, bool forceRandom = false, byte minRare = 0, bool isRaidbox = false)
        {
            if (Session.Character.Inventory != null && Session != null)
            {
                ItemInstance newItem = Inventory.InstantiateItemInstance(itemVNum, Session.Character.CharacterId, amount);
                if (newItem.Item == null)
                {
                    //await //LOGGER($"[ItemThread] {itemVNum} does not exist");
                    return;
                }

                if (newItem.Item.VNum.Equals(null))
                {
                    return;
                }

                newItem.Design = design;

                if (newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon || newItem.Item.ItemType == ItemType.Shell || forceRandom)
                {
                    if (rare != 0 && !forceRandom)
                    {
                        try
                        {
                            int FirstChance = ServerManager.RandomNumber(1, 100);
                            if (FirstChance < 10)
                            {
                                int SecondChance = ServerManager.RandomNumber(1, 10);
                                if (SecondChance < 5)
                                {
                                    newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, true, 8);
                                }
                                else
                                {
                                    newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, true, 7);
                                }
                            }
                            else
                            {
                                newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, false, 0, 6);
                            }
                            newItem.Upgrade = (byte)(newItem.Item.BasicUpgrade + upgrade);
                            if (newItem.Upgrade > 10)
                            {
                                newItem.Upgrade = 10;
                            }
                        }
                        catch
                        {
                            throw;
                        }
                    }
                    else if (rare == 0 || forceRandom)
                    {
                        do
                        {
                            try
                            {
                                newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None);
                                newItem.Upgrade = newItem.Item.BasicUpgrade;
                                if (newItem.Rare >= minRare)
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                break;
                            }
                        } while (forceRandom);
                    }
                }

                if (newItem.Item.Type.Equals(InventoryType.Equipment) && rare != 0 && !forceRandom)
                {
                    if (isRaidbox)
                    {
                        newItem.Rare = (sbyte)ServerManager.RandomNumber(5, 8);
                        newItem.SetRarityPoint();
                    }
                    else
                    {
                        newItem.Rare = (sbyte)rare;
                        newItem.SetRarityPoint();
                    }
                }

                if (newItem.Item.ItemType == ItemType.Shell)
                {
                    newItem.Upgrade = (byte)ServerManager.RandomNumber(50, 81);
                }

                if (newItem.Item.EquipmentSlot == EquipmentType.Gloves || newItem.Item.EquipmentSlot == EquipmentType.Boots)
                {
                    newItem.Upgrade = upgrade;
                    newItem.DarkResistance = (short)(newItem.Item.DarkResistance * upgrade);
                    newItem.LightResistance = (short)(newItem.Item.LightResistance * upgrade);
                    newItem.WaterResistance = (short)(newItem.Item.WaterResistance * upgrade);
                    newItem.FireResistance = (short)(newItem.Item.FireResistance * upgrade);
                }

                if (newItem.Item.ItemType == ItemType.Jewelery && newItem.Item.ItemSubType == 3)
                {
                    newItem.ElementRate = design;
                }

                List<ItemInstance> newInv = Session.Character.Inventory.AddToInventory(newItem);
                if (newInv.Count > 0)
                {
                    if (newItem.Item.IsHeroic && newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon && newItem.Rare > 0)
                    {
                        newItem.GenerateHeroicShell(RarifyProtection.RandomHeroicAmulet);
                        newItem.SetRarityPoint();
                    }

                    MessageExtension.SendGreen(Session, $"You have received this Item - {newItem.Item.Name} x{amount}");
                    //await //LOGGER(Session.Character.CharacterId, Session.Character.Name, $"Received Item: {newItem.Item.Name} x{amount}", LogType.Event);
                }
                else if (Session.Character.MailList.Count(s => s.Value.AttachmentVNum != null) < 40)
                {
                    Send(Session, Session.Character.CharacterId, itemVNum, amount, newItem.Rare, newItem.Upgrade, newItem.Design, false);
                    MessageExtension.SendGreen(Session, $"You received a new Parcel - {newItem.Item.Name} x{amount}");
                    //await //LOGGER(Session.Character.CharacterId, Session.Character.Name, $"Received Parcel: {newItem.Item.Name} x{amount}", LogType.Event);
                }
            }

        }

        
         
        public static void Send(ClientSession Session, long id, short vnum, short amount, sbyte rare, byte upgrade, short design, bool isNosmall)
        {
            Item it = ServerManager.GetItem(vnum);

            if (it != null)
            {
                if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor &&
                    it.ItemType != ItemType.Specialist && it.EquipmentSlot != EquipmentType.Gloves &&
                    it.EquipmentSlot != EquipmentType.Boots)
                {
                    upgrade = 0;
                }
                else if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor)
                {
                    rare = 0;
                }

                if (rare > 8 || rare < -2)
                {
                    rare = 0;
                }

                if (upgrade > 10 && it.ItemType != ItemType.Specialist)
                {
                    upgrade = 0;
                }
                else if (it.ItemType == ItemType.Specialist && upgrade > 15)
                {
                    upgrade = 0;
                }

                // maximum size of the amount is 32767
                if (amount > InventoryConfigrationExtension.MaxItemPerSlot)
                {
                    amount = InventoryConfigrationExtension.MaxItemPerSlot;
                }

                MailDTO mail = new MailDTO
                {
                    AttachmentAmount = it.Type == InventoryType.Etc || it.Type == InventoryType.Main
                        ? amount
                        : (short)1,
                    IsOpened = false,
                    Date = DateTime.Now,
                    ReceiverId = id,
                    SenderId = Session.Character.CharacterId,
                    AttachmentRarity = (byte)rare,
                    AttachmentUpgrade = upgrade,
                    AttachmentDesign = design,
                    IsSenderCopy = false,
                    Title = isNosmall ? "NOSMALL" : Session.Character.Name,
                    AttachmentVNum = vnum,
                    SenderClass = Session.Character.Class,
                    SenderGender = Session.Character.Gender,
                    SenderHairColor = Session.Character.HairColor,
                    SenderHairStyle = Session.Character.HairStyle,
                    EqPacket = Session.Character.GenerateEqListForPacket(),
                    SenderMorphId = Session.Character.Morph == 0 ? (short)-1 : (short)(Session.Character.Morph > short.MaxValue ? 0 : Session.Character.Morph)
                };
                MailServiceClient.Instance.SendMail(mail);
            }
        }


    }
}
