using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject.Helpers;

namespace NosGm.GameObject.Extension.Inventory
{
    public static class RemoveRuneExtension
    {
        #region Methods

        public static void RemoveRune(this ItemInstance e, ClientSession s)
        {
            if (e.Item.EquipmentSlot != EquipmentType.MainWeapon && e.Item.EquipmentSlot != EquipmentType.SecondaryWeapon)
            {
                // Not Main weapon
                s.SendShopEnd();
                return;
            }


            if (s.Character.Inventory.CountItem(5812) < 1)
            {
                // Not Enough Item
                s.SendShopEnd();
                return;
            }

            e.RuneEffects.Clear();
            e.RuneAmount = 0;
            e.IsBreaked = false;
            DAOFactory.RuneEffectDAO.DeleteByEquipmentSerialId(e.EquipmentSerialId);
            var msg = $"The {e.Item.Name} runes have been removed";
            s.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            s.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            s.Character.Inventory.RemoveItemAmount(5812);
            s.SendPacket(e.GenerateInventoryAdd());
            s.SendPacket(s.Character.GenerateEq());
            s.SendPacket(s.Character.GenerateEquipment());
            s.SendShopEnd();
        }

        #endregion
    }
}