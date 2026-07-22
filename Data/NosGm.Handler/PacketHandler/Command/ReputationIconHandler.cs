using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Command
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