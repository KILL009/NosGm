using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G507 : IGuriHandler
    {
        public long GuriEffectId => 507;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            DialogExtension.GenerateDialog(Session, 12005);
        }
    }
}