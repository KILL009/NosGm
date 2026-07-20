using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class WarningHandler : IPacketHandler
    {
        #region Instantiation

        public WarningHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Warn(WarningPacket warningPacket)
        {
            if (warningPacket != null)
            {
                if (Session.Account?.Authority < AuthorityType.DEV)
                {
                    Session.SendPacket(Session.Character.GenerateSay(
                        "Direct warnings are disabled. Use $Sanction preview <CaseId> warning 0 <Character> <reason>.", 11));
                    return;
                }

                var characterName = warningPacket.CharacterName;
                var character = DAOFactory.CharacterDAO.LoadByName(characterName);
                if (character != null)
                {
                    var session = ServerManager.Instance.GetSessionByCharacterName(characterName);
                    session?.SendPacket(UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("WARNING"), warningPacket.Reason)));
                    Character.InsertOrUpdatePenalty(new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = warningPacket.Reason,
                        Penalty = PenaltyType.Warning,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now,
                        AdminName = Session.Character.Name
                    });
                    switch (DAOFactory.PenaltyLogDAO.LoadByAccount(character.AccountId)
                        .Count(p => p.Penalty == PenaltyType.Warning))
                    {
                        case 1:
                            break;

                        case 2:
                            Session.MuteMethod(characterName, "Auto-Warning mute: 2 strikes", 30);
                            break;

                        case 3:
                            Session.MuteMethod(characterName, "Auto-Warning mute: 3 strikes", 60);
                            break;

                        case 4:
                            Session.MuteMethod(characterName, "Auto-Warning mute: 4 strikes", 720);
                            break;

                        case 5:
                            Session.MuteMethod(characterName, "Auto-Warning mute: 5 strikes", 1440);
                            break;

                        case 69:
                            Session.BanMethod(characterName, 7, "LOL SIXTY NINE AMIRITE?", false);
                            break;

                        default:
                            Session.MuteMethod(characterName, "You've been THUNDERSTRUCK", 6969);
                            break;
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
                Session.SendPacket(Session.Character.GenerateSay(WarningPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}
