using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class BlPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BlPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void BlBlacklistAdd(BlPacket blPacket)
        {
            if (blPacket.CharacterName != null && ServerManager.Instance.GetSessionByCharacterName(blPacket.CharacterName) is ClientSession receiverSession)
            {
                new BlInsPacketHandler(Session).BlacklistAdd(new BlInsPacket { CharacterId = receiverSession.Character.CharacterId });
            }
        }

        #endregion
    }
}