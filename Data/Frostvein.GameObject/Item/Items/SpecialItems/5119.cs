using Frostvein.Algorithm;
using Frostvein.GameObject.ItemThread;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Items
{
    public static class VNum5119
    {
        public static async Task Execute(ClientSession Session, ItemInstance inv)
        {
            RaidboxThread.GenerateReward(Session, inv);
            Session.Character.Inventory.RemoveItemFromInventory(inv.Id, 1);
        }
    }
}
