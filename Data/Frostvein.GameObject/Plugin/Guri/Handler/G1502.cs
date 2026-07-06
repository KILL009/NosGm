using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
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