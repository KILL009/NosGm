using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G205 : IGuriHandler
    {
        public long GuriEffectId => 205;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 205)
            {
                if (e.Argument == 0 && short.TryParse(e.User.ToString(), out var slot))
                {
                    const int perfumeVnum = 1428;

                    var perfumeInventoryType = (InventoryType)e.Argument;

                    var equipmentInstance = Session.Character.Inventory.LoadBySlotAndType(slot, perfumeInventoryType);

                    if (equipmentInstance?.BoundCharacterId == null || equipmentInstance.BoundCharacterId == Session.Character.CharacterId || equipmentInstance.Item.ItemType != ItemType.Weapon && equipmentInstance.Item.ItemType != ItemType.Armor)
                    {
                        return;
                    }

                    int perfumesNeeded = ShellGeneratorHelper.Instance.PerfumeFromItemLevelAndShellRarity(((short)(equipmentInstance.Item.IsHeroic ? 95 + equipmentInstance.Item.LevelMinimum : equipmentInstance.Item.LevelMinimum)), (byte)equipmentInstance.Rare);
                    int goldNeeded = ShellGeneratorHelper.Instance.PerfumeGoldAmountFromLevel(equipmentInstance.Item.IsHeroic ? (short)105 : equipmentInstance.Item.LevelMinimum);


                    if (Session.Character.Inventory.CountItem(perfumeVnum) < perfumesNeeded)
                    {
                        return;
                    }

                    Session.Character.Inventory.RemoveItemAmount(perfumeVnum, perfumesNeeded);
                    Session.Character.Gold -= goldNeeded;

                    equipmentInstance.BoundCharacterId = Session.Character.CharacterId;

                    Session.SendPacket(Session.Character.GenerateGold());
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BOUND_TO_YOU"), 0));
                }
            }
        }
    }
}