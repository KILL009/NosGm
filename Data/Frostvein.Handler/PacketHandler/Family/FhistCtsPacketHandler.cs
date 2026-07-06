using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Family
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