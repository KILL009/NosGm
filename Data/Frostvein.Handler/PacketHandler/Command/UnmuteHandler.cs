using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using Frostvein.GameObject.Extension.Message;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class UnmuteHandler : IPacketHandler
    {
        #region Instantiation

        public UnmuteHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async void Unmute(UnmutePacket unmutePacket)
        {
            if (unmutePacket != null)
            {
                if (Session.Account?.Authority < AuthorityType.DEV)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Direct unmutes are disabled. Use $Sanction preview <CaseId> unmute 0 <Character> <reason>.", 11));
                    return;
                }

                var name = unmutePacket.CharacterName;
                var chara = DAOFactory.CharacterDAO.LoadByName(name);
                if (chara != null)
                {
                    if (ServerManager.Instance.PenaltyLogs.Any(s =>
                        s.AccountId == chara.AccountId && s.Penalty == (byte)PenaltyType.Muted
                                                       && s.DateEnd > DateTime.Now))
                    {
                        var log = ServerManager.Instance.PenaltyLogs.Find(s =>
                            s.AccountId == chara.AccountId && s.Penalty == (byte)PenaltyType.Muted
                                                           && s.DateEnd > DateTime.Now);
                        if (log != null)
                        {
                            log.DateEnd = DateTime.Now.AddSeconds(-1);
                            Character.InsertOrUpdatePenalty(log);
                        }

                        MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_MUTED"), 10));
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
                Session.SendPacket(Session.Character.GenerateSay(UnmutePacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}
