using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G202 : IGuriHandler
    {
        public long GuriEffectId => 202;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket($"say 1 0 10 Soon will available, now is lock");
            return;
            if (e.Type == 202)
            {
                //Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PARTNER_BACKPACK"), 10));
                Session.SendPacket(Session.Character.GeneratePStashAll());
            }
        }
    }
}