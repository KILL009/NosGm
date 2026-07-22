using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G10 : IGuriHandler
    {
        public long GuriEffectId => 10;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            var output = e.Data == 1000 ? 5116 : e.Data + 4099;
            if (e.Type == 10 && e.Data >= 973 && e.Data <= 1000 && !Session.Character.EmoticonsBlocked)
            {
                if (e.User == Session.Character.CharacterId)
                {
                    Session.CurrentMapInstance?.Broadcast(Session,
                        StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId,
                            output), ReceiverType.AllNoEmoBlocked);
                }
                else if (int.TryParse(e.User.ToString(), out int mateTransportId))
                {
                    Mate mate = Session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                    if (mate != null)
                    {
                        Session.CurrentMapInstance?.Broadcast(Session,
                            StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId,
                                output), ReceiverType.AllNoEmoBlocked);
                    }
                }
            }
        }
    }
}