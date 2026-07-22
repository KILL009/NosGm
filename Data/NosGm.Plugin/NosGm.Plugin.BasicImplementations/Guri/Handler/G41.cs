using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G41 : IGuriHandler
    {
        public long GuriEffectId => 41;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket("scene 41 1");
        }
    }
}