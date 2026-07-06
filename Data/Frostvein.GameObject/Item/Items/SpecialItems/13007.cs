using Frostvein.Algorithm;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Items
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
