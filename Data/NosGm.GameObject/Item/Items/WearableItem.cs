
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Core.Extensions;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Diagnostics;
using System.Linq;

using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject
{
    public class WearableItem : Item
    {
        #region Instantiation

        public WearableItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            switch (Effect)
            {
                default:
                    var delay = false;
                    var bound = false;
                    if (Option == 255)
                    {
                        delay = true;
                        Option = 0;
                    }

                    Mate mate = null;
                    if (Option != 0)
                    {
                        mate = session.Character.Mates.Find(s => s.MateType == MateType.Partner && s.PetId == Option - 1);
                        if (mate == null || mate.IsTemporalMate)
                        {
                            return;
                        }
                    }

                    var slot = inv.Slot;
                    var equipment = InventoryType.Wear;
                    if (Option > 0)
                    {
                        equipment = (InventoryType)(12 + Option);
                    }

                    var itemToWearType = inv.Type;

                    if (inv == null)
                    {
                        return;
                    }

                    if (ItemValidTime > 0 && !inv.IsBound)
                    {
                        inv.ItemDeleteTime = DateTime.Now.AddSeconds(ItemValidTime);
                    }
                    else if (!inv.IsBound && ItemType == ItemType.Jewelery && new int[] { 3951, 3952, 3953, 3954, 3955, 7427 }.Contains(EffectValue))
                    {
                        inv.ItemDeleteTime = DateTime.Now.AddSeconds(3600);
                    }

                    if (!inv.IsBound)
                    {
                        switch (inv.Item.Effect)
                        {
                            case 790: // Tarot
                            case 932: // Attack amulet
                            case 933: // defense amulet
                                inv.BoundCharacterId = session.Character.CharacterId;
                                break;
                        }

                        if (!delay &&
                            (EquipmentSlot == EquipmentType.Fairy && (MaxElementRate == 70 || MaxElementRate == 80) ||
                             EquipmentSlot == EquipmentType.CostumeHat || EquipmentSlot == EquipmentType.CostumeSuit ||
                             EquipmentSlot == EquipmentType.WeaponSkin))
                        {
                            session.SendPacket(
                                $"qna #u_i^1^{session.Character.CharacterId}^{(byte)itemToWearType}^{slot}^1 {Language.Instance.GetMessageFromKey("ASK_BIND")}");
                            return;
                        }

                        if (delay)
                        {
                            inv.BoundCharacterId = session.Character.CharacterId;
                        }
                    }

                    var timeSpanSinceLastSpUsage =
                        (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds -
                        session.Character.LastSp;

                    if (EquipmentSlot == EquipmentType.Sp && inv.Rare == -2)
                    {
                        session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("CANT_EQUIP_DESTROYED_SP"), 0));
                        return;
                    }

                    if (Option == 0)
                    {
                        if (EquipmentSlot == EquipmentType.Sp &&
                            timeSpanSinceLastSpUsage <= session.Character.SpCooldown &&
                            session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp,
                                InventoryType.Specialist) != null)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("SP_INLOADING"),
                                    session.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage)), 0));
                            return;
                        }

                        if (ItemType != ItemType.Weapon
                            && ItemType != ItemType.Armor
                            && ItemType != ItemType.Fashion
                            && ItemType != ItemType.Jewelery
                            && ItemType != ItemType.Specialist
                            || LevelMinimum > (IsHeroic ? session.Character.HeroLevel : session.Character.Level) ||
                            Sex != 0 && Sex != (byte)session.Character.Gender + 1
                            || ItemType != ItemType.Jewelery && EquipmentSlot != EquipmentType.Boots &&
                            EquipmentSlot != EquipmentType.Gloves &&
                            ((Class >> (byte)session.Character.Class) & 1) != 1)
                        {
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"),
                                    10));
                            return;
                        }

                        if ((EquipmentSlot == EquipmentType.MainWeapon || EquipmentSlot == EquipmentType.Armor || EquipmentSlot == EquipmentType.SecondaryWeapon) && IsHeroic && inv.BoundCharacterId == null)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)itemToWearType}^{slot}^1 {Language.Instance.GetMessageFromKey("ASK_BIND")}");
                            return;
                        }

                        if (session.Character.UseSp)
                        {
                            if (session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, equipment) is
                                        ItemInstance sp && sp.Item.Element != 0 && EquipmentSlot == EquipmentType.Fairy &&
                                Element != sp.Item.Element && Element != sp.Item.SecondaryElement)
                            {
                                session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAD_FAIRY"),
                                                0));
                                return;
                            }
                        }

                        if (ItemType == ItemType.Weapon || ItemType == ItemType.Armor)
                        {
                            if (inv.BoundCharacterId.HasValue && inv.BoundCharacterId.Value != session.Character.CharacterId)
                            {
                                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                                return;
                            }
                        }

                        // bullshit but i had to do this fast
                        if (inv.ItemVNum == 327 || inv.ItemVNum == 328 || inv.ItemVNum == 329 || inv.ItemVNum == 330)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                            return;
                        }

                        if (session.Character.UseSp && EquipmentSlot == EquipmentType.Sp)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("SP_BLOCKED"), 10));
                            return;
                        }

                        if (session.Character.JobLevel < LevelJobMinimum)
                        {
                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_JOB_LVL"), 10));
                            return;
                        }
                    }
                    else if (mate != null)
                    {
                        if (inv.Item.EquipmentSlot == EquipmentType.MainWeapon ||
                            inv.Item.EquipmentSlot == EquipmentType.SecondaryWeapon ||
                            inv.Item.EquipmentSlot == EquipmentType.Armor)
                        {
                            switch (mate.Monster.AttackClass)
                            {
                                case 0:
                                    if (inv.ItemVNum != 990 && inv.ItemVNum != 997)
                                    {
                                        session.SendPacket(
                                                session.Character.GenerateSay(
                                                        Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                                        return;
                                    }

                                    break;

                                case 1:
                                    if (inv.ItemVNum != 991 && inv.ItemVNum != 996)
                                    {
                                        session.SendPacket(
                                                session.Character.GenerateSay(
                                                        Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                                        return;
                                    }

                                    break;

                                case 2:
                                    if (inv.ItemVNum != 992 && inv.ItemVNum != 995)
                                    {
                                        session.SendPacket(
                                                session.Character.GenerateSay(
                                                        Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"), 10));
                                        return;
                                    }

                                    break;
                            }
                        }

                        if (mate.Level < LevelMinimum)
                        {
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"),
                                    10));
                            return;
                        }

                        var partnerEquipment = ServerManager.GetItem(inv.ItemVNum);

                        var isBadEquipment = false;

                        switch (partnerEquipment.EquipmentSlot)
                        {
                            case EquipmentType.MainWeapon:
                                {
                                    if (partnerEquipment.ItemSubType != 12)
                                    {
                                        isBadEquipment = true;
                                    }
                                    else
                                    {
                                        mate.WeaponInstance = inv;
                                    }
                                }
                                break;

                            case EquipmentType.Armor:
                                {
                                    if (partnerEquipment.ItemSubType != 4)
                                    {
                                        isBadEquipment = true;
                                    }
                                    else
                                    {
                                        mate.ArmorInstance = inv;
                                    }
                                }
                                break;

                            case EquipmentType.Gloves:
                                {
                                    mate.GlovesInstance = inv;
                                }
                                break;

                            case EquipmentType.Boots:
                                {
                                    mate.BootsInstance = inv;
                                }
                                break;

                            case EquipmentType.Sp:
                                {
                                    if (mate.IsUsingSp)
                                    {
                                        return;
                                    }

                                    if (partnerEquipment.ItemSubType != 4
                                     || !PartnerHelper.CanWearSp(mate.NpcMonsterVNum, inv.ItemVNum))
                                    {
                                        isBadEquipment = true;
                                    }
                                    else
                                    {
                                        if (!mate.CanUseSp())
                                        {
                                            var spRemainingCooldown = mate.GetSpRemainingCooldown();
                                            session.SendPacket(session.Character.GenerateSay(
                                                string.Format(Language.Instance.GetMessageFromKey("STAY_TIME"),
                                                    spRemainingCooldown), 11));
                                            session.SendPacket($"psd {spRemainingCooldown}");
                                            return;
                                        }

                                        mate.Sp = new PartnerSp(inv);
                                    }
                                }
                                break;
                        }

                        if (isBadEquipment)
                        {
                            session.SendPacket(
                                session.Character.GenerateSay(Language.Instance.GetMessageFromKey("BAD_EQUIPMENT"),
                                    10));
                            return;
                        }
                    }

                    var currentlyEquippedItem =
                        session.Character.Inventory.LoadBySlotAndType((short)EquipmentSlot, equipment);

                    if (currentlyEquippedItem == null)
                    {
                        // move from equipment to wear
                        session.Character.Inventory.MoveInInventory(inv.Slot, itemToWearType, equipment);
                        session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(itemToWearType, slot));
                        if (IsHeroic && inv.BoundCharacterId == null)
                        {
                            bound = true;
                            if (bound)
                            {
                                inv.BoundCharacterId = session.Character.CharacterId;
                            }
                        }
                    }
                    else
                    {
                        //LOGGER($"[ITEM] EquipmentTakeoff | {session.GenerateIdentity()} | ItemVNum: {currentlyEquippedItem.ItemVNum} | Upgrade: {currentlyEquippedItem.Upgrade} | Rare: {currentlyEquippedItem.Rare}");
                        // move from wear to equipment and back
                        session.Character.Inventory.MoveInInventory(currentlyEquippedItem.Slot, equipment,
                            itemToWearType, inv.Slot);
                        session.SendPacket(currentlyEquippedItem.GenerateInventoryAdd());
                        session.Character.EquipmentBCards.RemoveAll(o => o.ItemVNum == currentlyEquippedItem.ItemVNum);
                    }

                    //LOGGER($"[ITEM] EquipmentTakeoff | {session.GenerateIdentity()} | ItemVNum: {inv.ItemVNum} | Upgrade: {inv.Upgrade} | Rare: {inv.Rare}");
                    var raid = ServerManager.Instance.Raids
                        .FirstOrDefault(s => s.RequiredItems?.Any(obj => obj?.VNum == VNum) == true)?.Copy();

                    if (mate == null)
                    {
                        session.Character.EquipmentBCards.AddRange(inv.Item.BCards);

                        switch (inv.Item.ItemType)
                        {
                            case ItemType.Armor:
                                session.Character.ShellEffectArmor.Clear();

                                foreach (var dto in inv.ShellEffects)
                                {
                                    session.Character.ShellEffectArmor.Add(dto);
                                }

                                break;

                            case ItemType.Weapon:
                                switch (inv.Item.EquipmentSlot)
                                {
                                    case EquipmentType.MainWeapon:
                                        session.Character.ShellEffectMain.Clear();
                                        session.Character.RuneEffectMain.Clear();

                                        foreach (var dto in inv.ShellEffects.Where(s => !s.IsRune))
                                        {
                                            session.Character.ShellEffectMain.Add(dto);
                                        }

                                        foreach (var dto in inv.RuneEffects)
                                        {
                                            session.Character.RuneEffectMain.Add(dto);
                                        }
                                        break;

                                    case EquipmentType.SecondaryWeapon:
                                        session.Character.ShellEffectSecondary.Clear();
                                        foreach (var dto in inv.ShellEffects)
                                        {
                                            session.Character.ShellEffectSecondary.Add(dto);
                                        }
                                        break;

                                    case EquipmentType.Fairy:
                                        session.Character.FairyEnchantments.Clear();
                                        foreach (var dto in inv.FairyEnchantments)
                                        {
                                            session.Character.FairyEnchantments.Add(dto);
                                        }
                                        break;
                                }

                                break;
                        }
                    }
                    else
                    {
                        mate.BattleEntity.BCards.Clear();
                        mate.BattleEntity.BCards.AddRange(inv.Item.BCards);
                    }

                    if (Option == 0)
                    {
                        session.CurrentMapInstance?.Broadcast(session.Character.GenerateEq());
                        session.SendPacket(session.Character.GenerateEquipment());
                        session.SendPackets(session.Character.GenerateStatChar());
                        session.CurrentMapInstance?.Broadcast(session.Character.GeneratePairy());

                        if (EquipmentSlot == EquipmentType.Fairy)
                        {
                            var fairy = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy,
                                equipment);
                            session.SendPacket($"msg 3 Fairy Experience: {fairy.XP}/{CharacterHelper.LoadFairyXPData(fairy.ElementRate)}");
                        }

                        if (EquipmentSlot == EquipmentType.MainWeapon)
                        {
                            ItemInstance mainWeapon = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, equipment);
                        }

                        if (EquipmentSlot == EquipmentType.SecondaryWeapon)
                        {
                            ItemInstance secondaryWeapon = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, equipment);
                        }

                        if (EquipmentSlot == EquipmentType.Orb)
                        {
                            session.CurrentMapInstance?.Broadcast(session.Character.GenerateEff(7843), ReceiverType.All);
                        }

                        if (EquipmentSlot == EquipmentType.Amulet)
                        {
                            session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player,
                                session.Character.CharacterId, 39));
                            if (inv.Item.ItemValidTime != 3) inv.BoundCharacterId = session.Character.CharacterId;
                            if (inv.ItemDeleteTime > DateTime.Now || inv.DurabilityPoint > 0)
                            {
                                session.Character.AddBuff(new Buff(62, session.Character.Level), session.Character.BattleEntity);
                            }                            
                        }
                    }
                    else if (mate != null)
                    {
                        session.SendPacket(mate.GenerateScPacket());
                    }

                    break;
            }
        }

        #endregion
    }
}