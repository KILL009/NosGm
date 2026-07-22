using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Event;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G505 : IGuriHandler
    {
        public long GuriEffectId => 505;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            MessageExtension.SendGrey(Session, "");
        }
    }
}