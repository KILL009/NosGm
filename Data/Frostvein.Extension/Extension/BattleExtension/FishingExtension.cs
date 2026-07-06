using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.Extension.Extension.BattleExtension
{
    public static class FishingExtension
    {
        public static bool CanFish(Character character)
        {
            ItemInstance sp = character.Inventory.LoadBySlotAndType((short)EquipmentType.Sp, InventoryType.Wear);

            if (sp == null || !character.UseSp)
                return false;

            if (sp.Item.Morph != 35 && sp.Item.Morph != 36)
                return false;

            var fishingPos = ServerManager.Instance.FishingPosition.FirstOrDefault(m => m.MapId == character.MapId && m.MapX == character.PositionX && m.MapY == character.PositionY && m.Direction == character.Direction);

            if (fishingPos == null)
                return false;

            if (sp.SpLevel < fishingPos.MinLevel)
                return false;

            return true;
        }
    }
}
