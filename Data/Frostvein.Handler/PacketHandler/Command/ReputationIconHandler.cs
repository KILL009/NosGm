using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ReputationIconHandler : IPacketHandler
    {
        public ReputationIconHandler (ClientSession session) => Session = session;

        public ClientSession Session { get; }

        public void ChangeReputationIcon(ReputationIconPacket reputationIconPacket)
        {
            Session.Character.Icon = reputationIconPacket.Type;
            Session.SendPacket(Session.Character.GenerateFd());
            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
        }
    }
}