using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
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