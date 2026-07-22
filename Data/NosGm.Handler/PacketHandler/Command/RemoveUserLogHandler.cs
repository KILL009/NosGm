using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class RemoveUserLogHandler : IPacketHandler
    {
        #region Instantiation

        public RemoveUserLogHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task RemoveUserLog(RemoveUserLogPacket removeUserLogPacket)
        {
            if (removeUserLogPacket == null
                || string.IsNullOrEmpty(removeUserLogPacket.Username))
                return;

            //Session.AddLogsCmd(removeUserLogPacket);
            if (ClientSession.UserLog.Contains(removeUserLogPacket.Username))
                ClientSession.UserLog.RemoveAll(username => username == removeUserLogPacket.Username);

            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}