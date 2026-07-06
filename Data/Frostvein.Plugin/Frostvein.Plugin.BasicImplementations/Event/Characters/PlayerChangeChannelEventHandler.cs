using ChickenAPI.Events;
using Frostvein.GameObject;
using Frostvein.GameObject.Characters.Events;
using Frostvein.Master.Library.Client;
using System.Threading;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Event.Characters
{
    public class PlayerChangeChannelEventHandler : GenericEventHandlerBase<PlayerChangeChannelEvent>
    {
        protected override void Handle(PlayerChangeChannelEvent e, CancellationToken cancellation)
        {
            Character character = e.Sender.Character;

            string ip = e.Ip;
            int port = e.Port;
            byte mode = e.Mode;

            ChangeChannel(character, ip, port, mode);
        }

        private void ChangeChannel(Character character, string ip, int port, byte mode)
        {

            character.Session.SendPacket($"mz {ip} {port} {character.Slot}");
            character.Session.SendPacket($"it {mode}");

            CommunicationServiceClient.Instance.RegisterCrossServerAccountLogin(character.Session.Account.AccountId, character.Session.SessionId);

            character.Session.PrepareDisconnection();

            character.Session.Disconnect();
        }
    }
}
