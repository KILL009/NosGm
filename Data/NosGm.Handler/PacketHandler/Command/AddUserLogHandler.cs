using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;

namespace NosGm.Handler.PacketHandler.Command
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
            try
            {
                if (addUserLogPacket == null
                    || string.IsNullOrEmpty(addUserLogPacket.Username))
                    return;
                ClientSession.UserLog.Add(addUserLogPacket.Username);

                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("DONE"), 10));
                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            catch { }
        }

        #endregion
    }
}