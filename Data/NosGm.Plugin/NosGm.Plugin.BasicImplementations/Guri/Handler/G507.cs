using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Event;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
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