using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class WearPacketHandler : IPacketHandler
    {
        #region Instantiation

        public WearPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Wear(WearPacket wearPacket)
        {
            if (wearPacket == null || Session.Character.ExchangeInfo?.ExchangeList.Count > 0
                                   || Session.Character.Speed == 0)
                return;

            if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.UserShops
                    .FirstOrDefault(mapshop => mapshop.Value.OwnerId.Equals(Session.Character.CharacterId)).Value
                == null)
            {
                var inv =
                    Session.Character.Inventory.LoadBySlotAndType(wearPacket.InventorySlot, InventoryType.Equipment);
                if (inv?.Item != null)
                {
                    var mate = Session.Character.Mates.Find(s => s.MateType == MateType.Partner && s.PetId == wearPacket.Type - 1);
                    if (mate != null)
                    {
                        ItemInstance mateinventory = null;
                        switch (inv.Item.EquipmentSlot)
                        {
                            case EquipmentType.Armor:
                                mateinventory = mate.ArmorInstance;
                                break;

                            case EquipmentType.MainWeapon:
                                mateinventory = mate.WeaponInstance;
                                break;

                            case EquipmentType.Gloves:
                                mateinventory = mate.GlovesInstance;
                                break;

                            case EquipmentType.Boots:
                                mateinventory = mate.BootsInstance;
                                break;
                            default:
                                mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == (inv.HoldingVNum == 0 ? inv.ItemVNum : inv.HoldingVNum));
                                break;

                        }
                        if (mateinventory != null)
                        {
                            mate.BattleEntity.BCards.RemoveAll(o => o.ItemVNum == (mateinventory.HoldingVNum == 0 ? mateinventory.ItemVNum : mateinventory.HoldingVNum));
                        }
                    }
                    inv.Item.Use(Session, inv, wearPacket.Type);
                    Session.Character.LoadSpeed();
                    Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId,
                        123));

                    var ring = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Ring,
                        InventoryType.Wear);
                    var bracelet =
                        Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Bracelet,
                            InventoryType.Wear);
                    var necklace =
                        Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Necklace,
                            InventoryType.Wear);

                    Session.Character.CellonOptions.Clear();

                    if (ring != null)
                    {
                        Session.Character.CellonOptions.AddRange(ring.CellonOptions);
                    }
                    if (bracelet != null)
                    {
                        Session.Character.CellonOptions.AddRange(bracelet.CellonOptions);
                    }
                    if (necklace != null)
                    {
                        Session.Character.CellonOptions.AddRange(necklace.CellonOptions);
                    }

                    Session.SendPacket(Session.Character.GenerateStat());
                    if (inv?.Item.EquipmentSlot == 0)
                    {
                        return;
                    }

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

        #endregion
    }
}