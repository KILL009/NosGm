using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G506 : IGuriHandler
    {
        public long GuriEffectId => 506;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e != null)
            {
                Session.Character.IsWaitingForEvent |= ServerManager.Instance.EventInWaiting;
            }
        }
    }
}