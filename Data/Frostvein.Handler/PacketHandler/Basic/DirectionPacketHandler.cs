using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Basic
{
    public class DirectionPacketHandler : IPacketHandler
    {
        #region Instantiation

        public DirectionPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Dir(DirectionPacket directionPacket)
        {
            if (directionPacket.CharacterId == Session.Character.CharacterId)
            {
                Session.Character.Direction = directionPacket.Direction;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateDir());
            }
        }

        #endregion
    }
}