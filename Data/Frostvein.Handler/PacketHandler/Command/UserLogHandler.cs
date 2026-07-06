using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class UserLogHandler : IPacketHandler
    {
        #region Instantiation

        public UserLogHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task UserLog(UserLogPacket userLogPacket)
        {
            if (userLogPacket == null) return;

            //Session.AddLogsCmd(userLogPacket);
            var n = 1;

            foreach (var username in ClientSession.UserLog)
                Session.SendPacket(Session.Character.GenerateSay($"{n++}- {username}", 12));

            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}