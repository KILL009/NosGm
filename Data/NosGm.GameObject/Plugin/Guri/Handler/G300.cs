using NosGm.Packets.Packets.ServerPackets;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G300 : IGuriHandler
    {
        public long GuriEffectId => 300;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            BuyPacket buyPacket = new BuyPacket();
            if (e.Type == 300)
            {
                if (e.Argument == 8023 && short.TryParse(e.User.ToString(), out var slot))
                {
                    var box = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);
                    if (box != null)
                    {
                        box.Item.Use(Session, box, 1, new[] { e.Data.ToString() });
                    }
                }
            }
        }
    }
}