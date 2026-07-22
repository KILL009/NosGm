using NosGm.Extension.Extension.Command;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
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
                if (Session.Account?.Authority < AuthorityType.DEV)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Direct bans are disabled. Use $Sanction preview <CaseId> ban <days> <Character> <reason>.", 11));
                    return;
                }

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
