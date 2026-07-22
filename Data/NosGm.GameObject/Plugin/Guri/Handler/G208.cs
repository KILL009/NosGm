using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G208 : IGuriHandler
    {
        public long GuriEffectId => 208;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 208 && e.Argument == 0)
            {
                if (short.TryParse(e.Value, out var mountSlot)
                    && short.TryParse(e.User.ToString(), out var pearlSlot))
                {
                    var mount = Session.Character.Inventory.LoadBySlotAndType(mountSlot, InventoryType.Main);
                    var pearl = Session.Character.Inventory.LoadBySlotAndType(pearlSlot, InventoryType.Equipment);

                    if (mount?.Item == null || pearl?.Item == null)
                    {
                        return;
                    }

                    if (!pearl.Item.IsHolder)
                    {
                        return;
                    }

                    if (pearl.HoldingVNum > 0)
                    {
                        return;
                    }

                    if (pearl.Item.ItemType == ItemType.Box && pearl.Item.ItemSubType == 4)
                    {
                        if (mount.Item.ItemType != ItemType.Special || mount.Item.ItemSubType != 0 || mount.Item.Speed < 1)
                        {
                            return;
                        }

                        Session.Character.Inventory.RemoveItemFromInventory(mount.Id);

                        pearl.HoldingVNum = mount.ItemVNum;

                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MOUNT_SAVED"), 0));
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MOUNT_SAVED"), 10));
                        Session.SendPacket("guri 25");
                    }
                }
            }
        }
    }
}