using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G42 : IGuriHandler
    {
        public long GuriEffectId => 42;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket("scene 42 1");
        }
    }
}