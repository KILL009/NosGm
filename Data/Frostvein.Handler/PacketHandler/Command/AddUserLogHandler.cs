using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class AddUserLogHandler : IPacketHandler
    {
        #region Instantiation

        public AddUserLogHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async void AddUserLog(AddUserLogPacket addUserLogPacket)
        {
            if (addUserLogPacket == null
                || string.IsNullOrEmpty(addUserLogPacket.Username))
                return;
            ClientSession.UserLog.Add(addUserLogPacket.Username);

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}