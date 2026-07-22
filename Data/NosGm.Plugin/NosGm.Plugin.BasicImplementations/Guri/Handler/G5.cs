using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G5 : IGuriHandler
    {
        public long GuriEffectId => 5;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            // Useless Just a Simple Dance Packet
        }
    }
}