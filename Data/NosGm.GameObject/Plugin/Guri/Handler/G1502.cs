using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G1502 : IGuriHandler
    {
        public long GuriEffectId => 1502;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            Session.SendPacket("info Something will come");
        }
    }
}