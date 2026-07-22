using NosGm.GameObject.ItemThread;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Items
{

    public static class VNum5989
    {
        public static async Task Execute(ClientSession Session, ItemInstance inv)
        {
            RaidboxThread.GenerateReward(Session, inv);
        }

    }
}
