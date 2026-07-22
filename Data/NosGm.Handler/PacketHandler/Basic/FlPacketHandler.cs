using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class FlPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FlPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FlRelationAdd(FlPacket flPacket)
        {
            if (flPacket.CharacterName != null && ServerManager.Instance.GetSessionByCharacterName(flPacket.CharacterName) is ClientSession receiverSession)
            {
                new FInsPacketHandler(Session).RelationAdd(new FInsPacket { Type = 1, CharacterId = receiverSession.Character.CharacterId });
            }
        }

        #endregion
    }
}