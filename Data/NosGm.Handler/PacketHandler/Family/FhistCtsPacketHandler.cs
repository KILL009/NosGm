using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Family
{
    public class FhistCtsPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FhistCtsPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyRefreshHist(FhistCtsPacket fhistCtsPacket)
        {
            Session.SendPackets(Session.Character.GetFamilyHistory());
        }

        #endregion
    }
}