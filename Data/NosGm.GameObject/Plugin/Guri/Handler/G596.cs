using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G596 : IGuriHandler
    {
        public long GuriEffectId => 596;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 596)
            {
                Session.Character.IsWaitingForEvent |= ServerManager.Instance.EventInWaiting;
            }
        }
    }
}