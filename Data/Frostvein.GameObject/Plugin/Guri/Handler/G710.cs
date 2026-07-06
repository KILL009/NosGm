using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G710 : IGuriHandler
    {
        public long GuriEffectId => 710;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 710)
            {
                if (e.Value != null)
                {
                    if (!Session.CurrentMapInstance.Npcs.Any(n => n.MapNpcId.Equals(e.Data)))
                    {
                        return;
                    }

                    Session.Character.TeleportOnMap((short)e.Argument, (short)e.Parameter);
                }
            }
        }
    }
}