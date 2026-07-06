using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject._Guri;
using Frostvein.GameObject._Guri.Event;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G2001 : IGuriHandler
    {
        public long GuriEffectId => 2001;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(e.User);

            if (Session.Character.Channel.ChannelId == 51) { Session.SendPacket("info You cannot duel in Glacernon."); return; }
            if (session.Character.Channel.ChannelId == 51) { session.SendPacket("info You cannot duel in Glacernon."); return; }
            if (Session.Character.IsExchanging) { Session.SendPacket("info You cannot duel while Trading"); return; }
            if (session.Character.IsExchanging) { session.SendPacket("info You cannot duel while Trading"); return; }
            if (Session.Character.IsShopping) { Session.SendPacket("info You cannot duel while Shopping"); return; }
            if (session.Character.IsShopping) { session.SendPacket("info You cannot duel while Shopping"); return; }
            if (Session.Character.IsSeal) { Session.SendPacket("info You cannot duel while being in a Raid"); return; }
            if (session.Character.IsSeal) { session.SendPacket("info You cannot duel while being in a Raid"); return; }
            if (Session.Character.Group != null && Session.Character.Group.GroupType == GroupType.Group) { Session.SendPacket("info You cannot duel while being in a Group"); return; }
            if (session.Character.Group != null && session.Character.Group.GroupType == GroupType.Group) { session.SendPacket("info You cannot duel while being in a Group"); return; }
            if (Session.Character.DuelCount == 5) { Session.SendPacket("info You already participated in a duel 5 times today"); return; }
            if (session.Character.DuelCount == 5) { session.SendPacket("info You already participated in a duel 5 times today"); return; }
            //if (Session.IpAddress == session.IpAddress) { Session.SendPacket("info Matching IP detected. Please contact us if this is not your own Character."); return; }
            //if (session.IpAddress == Session.IpAddress) { Session.SendPacket("info Matching IP detected. Please contact us if this is not your own Character."); return; }

            if (e.Argument == 0)
            {
                Session.Character.IsIn1v1PrivateQueue = true;
                session.Character.IsIn1v1PrivateQueue = true;
                DuelEventPrivate.GenerateOneVersusOne();
            }
            else
            {
                Session.SendPacket("info You declined the Duel.");
                session.SendPacket($"info {Session.Character.Name} declined the Duel.");
            }
        }
    }
}