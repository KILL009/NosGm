using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G513 : IGuriHandler
    {
        public long GuriEffectId => 513;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 513)
            {
                if (Session?.Character?.MapInstance == null)
                {
                    return;
                }

                if (Session.Character.IsLaurenaMorph())
                {
                    Session.Character.MapInstance.Broadcast(Session.Character.GenerateEff(4054));
                    Session.Character.ClearLaurena();
                }
            }
        }
    }
}