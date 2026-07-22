using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Inventory
{
    public class SpecialistHolderPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SpecialistHolderPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SpecialistHolder(SpecialistHolderPacket specialistHolderPacket)
        {
            if (specialistHolderPacket != null)
            {
                ItemInstance specialist = Session.Character.Inventory.LoadBySlotAndType(specialistHolderPacket.Slot, InventoryType.Equipment);

                ItemInstance holder = Session.Character.Inventory.LoadBySlotAndType(specialistHolderPacket.HolderSlot,
                    InventoryType.Equipment);

                if (specialist == null || holder == null)
                {
                    return;
                }

                if (!holder.Item.IsHolder)
                {
                    return;
                }

                if (holder.HoldingVNum > 0)
                {
                    return;
                }

                if (specialist.Item.ItemType != ItemType.Specialist || holder.Item.ItemType != ItemType.Box)
                {
                    return;
                }

                if (specialistHolderPacket.HolderType == 0)
                {
                    if (holder.Item.ItemType == ItemType.Box && holder.Item.ItemSubType == 2)
                    {
                        if (specialist.Item.ItemType != ItemType.Specialist || !specialist.Item.IsSoldable ||
                            specialist.Item.Class == 0)
                        {
                            return;
                        }

                        if (specialist.ItemVNum >= 4494 && specialist.ItemVNum <= 4496)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_HOLD_SP")));
                            Session.SendPacket("shop_end 2");
                            return;
                        }

                        Session.Character.Inventory.RemoveItemFromInventory(specialist.Id);

                        holder.HoldingVNum = specialist.ItemVNum;
                        holder.SlDamage = specialist.SlDamage;
                        holder.SlDefence = specialist.SlDefence;
                        holder.SlElement = specialist.SlElement;
                        holder.SlHP = specialist.SlHP;
                        holder.SpDamage = specialist.SpDamage;
                        holder.SpDark = specialist.SpDark;
                        holder.SpDefence = specialist.SpDefence;
                        holder.SpElement = specialist.SpElement;
                        holder.SpFire = specialist.SpFire;
                        holder.SpHP = specialist.SpHP;
                        holder.SpLevel = specialist.SpLevel;
                        holder.SpLight = specialist.SpLight;
                        holder.SpStoneUpgrade = specialist.SpStoneUpgrade;
                        holder.SpWater = specialist.SpWater;
                        holder.Upgrade = specialist.Upgrade;
                        holder.XP = specialist.XP;
                        holder.EquipmentSerialId = specialist.EquipmentSerialId;

                        Session.SendPacket("shop_end 2");
                    }
                }
                else if (specialistHolderPacket.HolderType == 1)
                {
                    if (holder.Item.ItemType != ItemType.Box && holder.Item.ItemSubType != 6)
                    {
                        return;
                    }

                    if (specialist.Item.ItemType != ItemType.Specialist || !specialist.Item.IsSoldable)
                    {
                        return;
                    }

                    Session.Character.Inventory.RemoveItemFromInventory(specialist.Id);

                    holder.HoldingVNum = specialist.ItemVNum;
                    holder.XP = specialist.XP;
                    holder.EquipmentSerialId = specialist.EquipmentSerialId;

                    Session.SendPacket("shop_end 2");
                }
            }
        }

        #endregion
    }
}