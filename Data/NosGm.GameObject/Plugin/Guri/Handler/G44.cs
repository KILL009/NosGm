using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G44 : IGuriHandler
    {
        public long GuriEffectId => 44;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket("scene 44 1");
        }
    }
}