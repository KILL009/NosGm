using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class MuteHandler : IPacketHandler
    {
        #region Instantiation

        public MuteHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Mute(MutePacket mutePacket)
        {
            if (mutePacket != null)
            {
                if (Session.Account?.Authority < AuthorityType.DEV)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Direct mutes are disabled. Use $Sanction preview <CaseId> mute <minutes> <Character> <reason>.", 11));
                    return;
                }

                if (mutePacket.Duration == 0) mutePacket.Duration = 60;

                mutePacket.Reason = mutePacket.Reason?.Trim();
                Session.MuteMethod(mutePacket.CharacterName, mutePacket.Reason, mutePacket.Duration);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(MutePacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}
