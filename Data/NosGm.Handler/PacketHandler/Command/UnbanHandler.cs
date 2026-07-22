using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class UnbanHandler : IPacketHandler
    {
        #region Instantiation

        public UnbanHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Unban(UnbanPacket unbanPacket)
        {
            if (unbanPacket != null)
            {
                if (Session.Account?.Authority < AuthorityType.DEV)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Direct unbans are disabled. Use $Sanction preview <CaseId> unban 0 <Character> <reason>.", 11));
                    return;
                }

                var name = unbanPacket.CharacterName;
                var chara = DAOFactory.CharacterDAO.LoadByName(name);
                if (chara != null)
                {
                    var log = ServerManager.Instance.PenaltyLogs.Find(s =>
                        s.AccountId == chara.AccountId && s.Penalty == PenaltyType.Banned && s.DateEnd > DateTime.Now);
                    if (log != null)
                    {
                        log.DateEnd = DateTime.Now.AddSeconds(-1);
                        Character.InsertOrUpdatePenalty(log);
                        MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_BANNED"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(UnbanPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}
