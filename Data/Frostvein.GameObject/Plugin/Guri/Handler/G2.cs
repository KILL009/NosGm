using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G2 : IGuriHandler
    {
        public long GuriEffectId => 2;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 2)
            {
                Session.CurrentMapInstance?.Broadcast(
                    UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId),
                    Session.Character.PositionX, Session.Character.PositionY);
            }
        }
    }
}