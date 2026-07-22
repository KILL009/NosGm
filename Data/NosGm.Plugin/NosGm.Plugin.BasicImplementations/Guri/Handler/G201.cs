using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using System.Linq;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G201 : IGuriHandler
    {
        public long GuriEffectId => 201;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 201)
            {
                if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
                {
                    Session.SendPacket(Session.Character.GenerateStashAll());
                }
            }
        }
    }
}