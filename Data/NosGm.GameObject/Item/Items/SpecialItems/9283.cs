using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.GameObject.Items
{
    public static class VNum9283
    {
        public static async Task Execute(ClientSession Session)
        {
            if (!Session.Character.HasPremiumBattlePass)
            {
                Session.Character.BattlePassAccountLogs.RemoveAll(w => w.AccountId == Session.Account.AccountId);
                Session.Character.HasPremiumBattlePass = true;
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId, Session.Character.MapX, Session.Character.MapY);
            }
            MessageExtension.SendGreen(Session, "Your Premium Batle Pass has been activated!");
            MessageExtension.SendGreen(Session, "Your Battle Pass has been resetted");
        }
    }
}
