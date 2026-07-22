using NosGm.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Items
{
    public static class VNum9071
    {
        public static async Task Execute(ClientSession Session)
        {
            Session.SendPacket("info Test");
        }
    }
}
