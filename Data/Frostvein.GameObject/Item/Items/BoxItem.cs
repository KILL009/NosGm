

using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.ItemThread;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Frostvein.GameObject
{
    public class BoxItem : Item
    {
        #region Instantiation

        public BoxItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            if (session.Character.IsVehicled && Effect != 888)
            {
                session.SendPacket(
                    session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (inv.ItemVNum == 333 || inv.ItemVNum == 334
            ) // Sealed Jajamaru Specialist Card & Sealed Princess Sakura Bead
                return;

            switch (Effect)
            {
                case 0:
                    switch (VNum)
                    {
                        case 4801:
                            var box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                            if (box != null)
                            {
                                if (box.HoldingVNum == 0)
                                {
                                    session.SendPacket($"wopen 44 {inv.Slot} 1");
                                }
                                else
                                {
                                    var newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                    if (newInv.Count > 0)
                                    {
                                        newInv[0].EquipmentSerialId = box.EquipmentSerialId;
                                        var itemInstance = newInv[0];
                                        var specialist =
                                            session.Character.Inventory.LoadBySlotAndType(itemInstance.Slot,
                                                itemInstance.Type);
                                        var Slot = inv.Slot;
                                        if (Slot != -1)
                                        {
                                            if (specialist != null)
                                            {
                                                session.SendPacket(session.Character.GenerateSay(
                                                    $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {specialist.Item.Name}",
                                                    12));
                                                newInv.ForEach(s =>
                                                    session.SendPacket(specialist.GenerateInventoryAdd()));
                                            }

                                            session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                        }
                                    }
                                    else
                                    {
                                        session.SendPacket(
                                            UserInterfaceHelper.GenerateMsg(
                                                Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                    }
                                }
                            }

                            return;
                    }

                    if (Option == 0)
                    {
                        if (packetsplit?.Length == 9)
                        {
                            var box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                            if (box != null)
                            {
                                if (box.Item.ItemSubType == 3)
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_OPEN_BOX")}");
                                }

                                else if (box.HoldingVNum == 0)
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot}^{packetsplit[3]} {Language.Instance.GetMessageFromKey("ASK_STORE_PET")}");
                                }

                                else
                                {
                                    session.SendPacket($"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_RELEASE_PET")}");
                                }

                            }
                        }
                    }
                    else
                    {
                        RaidboxThread.GenerateReward(session, inv);
                    }

                    break;

                case 1:
                    if (Option == 0)
                    {
                        session.SendPacket(
                            $"qna #guri^300^8023^{inv.Slot} {Language.Instance.GetMessageFromKey("ASK_RELEASE_PET")}");
                    }
                    else
                    {
                        var heldMonster = ServerManager.GetNpcMonster((short)EffectValue);
                        if (session.CurrentMapInstance == session.Character.Miniland && heldMonster != null)
                        {
                            var mate = new Mate(session.Character, heldMonster, LevelMinimum,
                                ItemSubType == 1 ? MateType.Partner : MateType.Pet);
                            if (session.Character.AddPet(mate))
                            {
                                if (mate.Name == "Otter")
                                {
                                    mate.Name = "Bober Kurwa";
                                    MessageExtension.SendBubble(session, "Your Pet is now named Bober Kurwa. Anyways, if you dont like the name.. here's a Name Tag. But the Name is good. Really, it's perfect.");
                                    session.Character.GiftAdd(10023, 1);
                                }
                                session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                session.SendPacket(
                                    UserInterfaceHelper.GenerateInfo(
                                        Language.Instance.GetMessageFromKey("PET_LEAVE_BEAD")));
                            }
                        }
                        else
                        {
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_IN_MINILAND"),
                                    12));
                        }
                    }

                    break;

                case 69:
                    if (EffectValue == 1 || EffectValue == 2)
                    {
                        ItemInstance box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"wopen 44 {inv.Slot}");
                            }
                            else
                            {
                                List<ItemInstance> newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    ItemInstance itemInstance = newInv[0];
                                    ItemInstance specialist = session.Character.Inventory.LoadBySlotAndType(itemInstance.Slot, itemInstance.Type);
                                    if (specialist != null)
                                    {
                                        specialist.SlDamage = box.SlDamage;
                                        specialist.SlDefence = box.SlDefence;
                                        specialist.SlElement = box.SlElement;
                                        specialist.SlHP = box.SlHP;
                                        specialist.SpDamage = box.SpDamage;
                                        specialist.SpDark = box.SpDark;
                                        specialist.SpDefence = box.SpDefence;
                                        specialist.SpElement = box.SpElement;
                                        specialist.SpFire = box.SpFire;
                                        specialist.SpHP = box.SpHP;
                                        specialist.SpLevel = box.SpLevel;
                                        specialist.SpLight = box.SpLight;
                                        specialist.SpStoneUpgrade = box.SpStoneUpgrade;
                                        specialist.SpWater = box.SpWater;
                                        specialist.Upgrade = box.Upgrade;
                                        specialist.EquipmentSerialId = box.EquipmentSerialId;
                                        specialist.XP = box.XP;
                                    }
                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        if (specialist != null)
                                        {
                                            session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {specialist.Item.Name} + {specialist.Upgrade}", 12));
                                            newInv.ForEach(s => session.SendPacket(specialist.GenerateInventoryAdd()));
                                        }
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    if (EffectValue == 3)
                    {
                        var box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 26 0 {inv.Slot}");
                            }
                            else
                            {
                                var newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    var itemInstance = newInv[0];
                                    var fairy = session.Character.Inventory.LoadBySlotAndType(itemInstance.Slot,
                                        itemInstance.Type);
                                    if (fairy != null) fairy.ElementRate = box.ElementRate;
                                    var Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        if (fairy != null)
                                        {
                                            session.SendPacket(session.Character.GenerateSay(
                                                $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {fairy.Item.Name} ({fairy.ElementRate}%)",
                                                12));
                                            newInv.ForEach(s => session.SendPacket(fairy.GenerateInventoryAdd()));
                                        }

                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }

                    if (EffectValue == 4)
                    {
                        var box = session.Character.Inventory.LoadBySlotAndType(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 24 0 {inv.Slot}");
                            }
                            else
                            {
                                var newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv.Count > 0)
                                {
                                    var Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        session.SendPacket(session.Character.GenerateSay(
                                            $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {newInv[0].Item.Name} x1)",
                                            12));
                                        newInv.ForEach(s => session.SendPacket(s.GenerateInventoryAdd()));
                                        session.Character.Inventory.RemoveItemFromInventory(box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }

                    break;

                case 888:
                    if (session.Character.IsVehicled)
                        if (!session.Character.Buff.Any(s => s.Card.CardId == 336))
                        {
                            if (inv.ItemDeleteTime == null) inv.ItemDeleteTime = DateTime.Now.AddHours(LevelMinimum);
                            session.Character.VehicleItem.BCards.ForEach(s =>
                                s.ApplyBCards(session.Character.BattleEntity, session.Character.BattleEntity));
                            session.CurrentMapInstance.Broadcast($"eff 1 {session.Character.CharacterId} 885");
                        }

                    break;

                default:
                    //LOGGER($"[HANDLER] Handler not found for: {GetType()} | {VNum} | {Effect} | {EffectValue}");
                    break;
            }
        }

        #endregion
    }
}