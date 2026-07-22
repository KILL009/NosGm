using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
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