using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System;
using System.Diagnostics;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class RemovePacketHandler : IPacketHandler
    {
        #region Instantiation

        public RemovePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Remove(RemovePacket removePacket)
        {
            if (removePacket != null)
            {
                InventoryType equipment;
                GameObject.Mate mate = null;
                if (removePacket.Type > 0)
                {
                    equipment = (InventoryType)(12 + removePacket.Type);
                    mate = Session.Character.Mates.Find(s =>
                        s.MateType == MateType.Partner && s.PetId == removePacket.Type - 1);
                    if (mate.IsTemporalMate) return;
                }
                else
                {
                    equipment = InventoryType.Wear;
                }

                if (Session.HasCurrentMapInstance
                    && Session.CurrentMapInstance.UserShops.FirstOrDefault(mapshop =>
                        mapshop.Value.OwnerId.Equals(Session.Character.CharacterId)).Value == null
                    && (Session.Character.ExchangeInfo == null
                        || (Session.Character.ExchangeInfo?.ExchangeList).Count == 0))
                {
                    var inventory =
                        Session.Character.Inventory.LoadBySlotAndType(removePacket.InventorySlot, equipment);
                    if (inventory != null)
                    {
                        var currentRunningSeconds =
                            (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                        var timeSpanSinceLastSpUsage = currentRunningSeconds - Session.Character.LastSp;
                        if (removePacket.Type == 0)
                        {
                            if (removePacket.InventorySlot == (byte)EquipmentType.Sp && Session.Character.UseSp &&
                                !Session.Character.IsSeal)
                            {
                                if (Session.Character.IsVehicled)
                                {
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("REMOVE_VEHICLE"), 0));
                                    return;
                                }

                                if (Session.Character.LastSkillUse.AddSeconds(2) > DateTime.Now) return;

                                if (Session.Character.Timespace != null &&
                                    Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != 0 &&
                                    Session.Character.Timespace.InstanceBag.Lock) return;

                                if (!Session.Character.RemoveSp(inventory.ItemVNum, false)) return;

                                Session.Character.LastSp =
                                    (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                            }
                            else if (removePacket.InventorySlot == (byte)EquipmentType.Sp
                                     && !Session.Character.UseSp
                                     && timeSpanSinceLastSpUsage <= Session.Character.SpCooldown)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("SP_INLOADING"),
                                        Session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage, 0)),
                                    0));
                                return;
                            }
                            else if (removePacket.InventorySlot == (byte)EquipmentType.Fairy && Session.Character.IsUsingFairyBooster)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("REMOVE_FAIRY_WHILE_USING_BOOSTER"), 0));
                                return;
                            }

                            if ((inventory.ItemDeleteTime >= DateTime.Now || inventory.DurabilityPoint > 0) &&
                                Session.Character.Buff.ContainsKey(62)) Session.Character.RemoveBuff(62);

                            Session.Character.EquipmentBCards.RemoveAll(o => o.ItemVNum == inventory.ItemVNum);
                        }

                        var inv = Session.Character.Inventory.MoveInInventory(removePacket.InventorySlot,
                            equipment, InventoryType.Equipment);

                        if (inv == null)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                    0));
                            return;
                        }

                        if (inv.Slot != -1) Session.SendPacket(inventory.GenerateInventoryAdd());

                        if (removePacket.Type == 0)
                        {                            
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateEq());
                            Session.SendPacket(Session.Character.GenerateEquipment());
                            Session.SendPackets(Session.Character.GenerateStatChar());
                            Session.CurrentMapInstance?.Broadcast(Session.Character.GeneratePairy());
                        }
                        else if (mate != null)
                        {
                            switch (inv.Item.EquipmentSlot)
                            {
                                case EquipmentType.Armor:
                                    mate.ArmorInstance = null;
                                    break;

                                case EquipmentType.MainWeapon:
                                    mate.WeaponInstance = null;
                                    break;

                                case EquipmentType.Gloves:
                                    mate.GlovesInstance = null;
                                    break;

                                case EquipmentType.Boots:
                                    mate.BootsInstance = null;
                                    break;

                                case EquipmentType.Sp:
                                    {
                                        if (mate.IsUsingSp)
                                        {
                                            mate.RemoveSp();
                                            mate.StartSpCooldown();
                                        }

                                        mate.Sp = null;
                                    }
                                    break;
                            }

                            mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == inventory.HoldingVNum);
                            Session.SendPacket(mate.GenerateScPacket());
                        }

                        var ring = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Ring,
                            InventoryType.Wear);
                        var bracelet =
                            Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Bracelet,
                                InventoryType.Wear);
                        var necklace =
                            Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Necklace,
                                InventoryType.Wear);

                        Session.Character.CellonOptions.Clear();

                        if (ring != null) Session.Character.CellonOptions.AddRange(ring.CellonOptions);
                        if (bracelet != null) Session.Character.CellonOptions.AddRange(bracelet.CellonOptions);
                        if (necklace != null) Session.Character.CellonOptions.AddRange(necklace.CellonOptions);
                        Session.SendPacket(Session.Character.GenerateStat());

                        if (inv.Item.EquipmentSlot == EquipmentType.MainWeapon ||
                            inv.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                        {
                            var sp =
                           Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp,
                               InventoryType.Wear);

                            if (sp != null && Session.Character.UseSp)
                            {
                                CharacterHelper.UpdateSPPoints(ref sp, Session);
                            }
                        }
                    }
                }
            }        
        }

        #endregion
    }
}