using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class BanHandler : IPacketHandler
    {
        #region Instantiation

        public BanHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Ban(BanPacket banPacket)
        {
            if (banPacket != null)
            {
                Session.BanMethod(banPacket.CharacterName, banPacket.Duration, banPacket.Reason, banPacket.IsBanIp);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(BanPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}