using NosGm.Algorithm;
using NosGm.Domain;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Items
{
    public static class VNum13007
    {
        public static async Task Execute(ClientSession Session)
        {
            if (!Session.Character.UnlockedBattlePassMultiplicator)
            {
                Session.Character.UnlockedBattlePassMultiplicator = true;
                MessageExtension.SendGreen(Session, "You unlocked the Battle Pass Multiplicator!");
            }
        }
    }
}
