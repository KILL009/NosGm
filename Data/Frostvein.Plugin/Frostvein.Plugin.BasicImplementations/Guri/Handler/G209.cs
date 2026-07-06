using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G209 : IGuriHandler
    {
        public long GuriEffectId => 209;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 209 && e.Argument == 0)
            {
                if (short.TryParse(e.Value, out var fairySlot)
                    && short.TryParse(e.User.ToString(), out var pearlSlot))
                {
                    var fairy = Session.Character.Inventory.LoadBySlotAndType(fairySlot, InventoryType.Equipment);
                    var pearl = Session.Character.Inventory.LoadBySlotAndType(pearlSlot, InventoryType.Equipment);

                    if (fairy?.Item == null || pearl?.Item == null)
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

                    if (pearl.Item.ItemType == ItemType.Box && pearl.Item.ItemSubType == 5)
                    {
                        if (fairy.Item.ItemType != ItemType.Jewelery || fairy.Item.ItemSubType != 3 || fairy.Item.IsDroppable)
                        {
                            return;
                        }

                        Session.Character.Inventory.RemoveItemFromInventory(fairy.Id);

                        pearl.HoldingVNum = fairy.ItemVNum;
                        pearl.ElementRate = fairy.ElementRate;

                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAIRY_SAVED"), 0));
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("FAIRY_SAVED"), 10));
                        Session.SendPacket("guri 27");
                    }
                }
            }
        }
    }
}