using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Family
{
    public class TodayPacketHandler : IPacketHandler
    {
        #region Instantiation

        public TodayPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyChangeMessage(TodayPacket todayPacket)
        {
            Session.SendPacket("today_stc");
        }

        #endregion
    }
}