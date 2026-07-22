using NosGm.Algorithm;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject.Items
{
    public static class VNum1458
    {
        public static async Task Execute(ClientSession Session)
        {
            if (Session.Character.Family == null)
            {
                Session.SendPacket("info You need a Family in order to use this Item");
                return;
            }
            if (Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head)
            {
                Session.SendPacket(Session.Character.GenerateSay("Only the Familyhead can use this item!", 11));
                return;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                Session.SendPacket(Session.Character.GenerateSay("You cannot use this Item in Glacernon!", 11));
                return;
            }

            foreach (var sess in ServerManager.Instance.Sessions.Where(x => x.Character.Family == Session.Character.Family && x != Session))
            {
                sess.SendPacket($"dlg #guri^2000^0^{Session.Character.CharacterId} #guri^2000^1^{Session.Character.CharacterId} {Session.Character.Name} used the Summoning Book. Do you wish to teleport to your Familyhead?");
                Session.Character.Inventory.RemoveItemAmount(1458, 1);
            }
        }
    }
}
