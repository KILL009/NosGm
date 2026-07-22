using NosGm.Packets.Packets.ClientPackets;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G2000 : IGuriHandler
    {
        public long GuriEffectId => 2000;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            var familyHead = ServerManager.Instance.GetCharacterById(Session.Character.CharacterId);

            if (familyHead == null)
            {
                Session.SendPacket("msg 4 Something went wrong. Seems like the Familyhead went offline!");
                return;
            }
            if (e.Argument == 0)
            {
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, familyHead.MapId, familyHead.MapX, familyHead.MapY);
            }
            else
            {
                familyHead.Session.SendPacket($"msg 4 {Session.Character.Name} declined your summoning.");
            }
        }
    }
}