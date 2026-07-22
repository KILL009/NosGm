using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class MapPvpHandler : IPacketHandler
    {
        #region Instantiation

        public MapPvpHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task MapPvp(MapPVPPacket mapPvpPacket)
        {
            //Session.AddLogsCmd(mapPvpPacket);
            Session.CurrentMapInstance.IsPVP = !Session.CurrentMapInstance.IsPVP;
            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}