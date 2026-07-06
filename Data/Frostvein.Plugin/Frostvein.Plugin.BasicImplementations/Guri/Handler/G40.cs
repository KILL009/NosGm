using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G40 : IGuriHandler
    {
        public long GuriEffectId => 40;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket("scene 40 1");
        }
    }
}