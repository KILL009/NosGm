using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G306 : IGuriHandler
    {
        public long GuriEffectId => 306;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 306)
            {
                TitleExtension.GenerateTitle(Session, e);
            }
        }
    }
}