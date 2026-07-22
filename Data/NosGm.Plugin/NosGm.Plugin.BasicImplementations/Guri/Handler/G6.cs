using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G6 : IGuriHandler
    {
        public long GuriEffectId => 6;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 6)
            {
                Mate mate = Session.Character.Mates.Find(s => s.IsTeamMember && s.IsAlive && s.MateType == NosGm.Domain.MateType.Partner);
                if (mate != null)
                {
                    Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(2, 2, mate.MateTransportId), mate.PositionX, mate.PositionY);
                }
            }
        }
    }
}
