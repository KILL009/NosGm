using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Extension.Extension.Command
{
    public static class CommandExt
    {
        public static void AddPortal(this ClientSession Session, short destinationMapId, short destinationX,
            short destinationY, short type,
            bool insertToDatabase)
        {
            if (Session.HasCurrentMapInstance)
            {
                var portal = new Portal
                {
                    SourceMapId = Session.Character.MapId,
                    SourceX = Session.Character.PositionX,
                    SourceY = Session.Character.PositionY,
                    DestinationMapId = destinationMapId,
                    DestinationX = destinationX,
                    DestinationY = destinationY,
                    DestinationMapInstanceId = insertToDatabase ? Guid.Empty :
                        destinationMapId == 20000 ? Session.Character.Miniland.MapInstanceId : Guid.Empty,
                    Type = type
                };
                if (insertToDatabase) DAOFactory.PortalDAO.Insert(portal);

                Session.CurrentMapInstance.Portals.Add(portal);
                Session.CurrentMapInstance?.Broadcast(portal.GenerateGp());
            }
        }

        public static async Task BanMethod(this ClientSession Session, string characterName, int duration, string reason, bool isBanIp)
        {
            var character = DAOFactory.CharacterDAO.LoadByName(characterName);
            if (character != null)
            {
                var sessionTarget = ServerManager.Instance.GetSessionByCharacterId(character.CharacterId);
                if (sessionTarget != null)
                {
                    ServerManager.Instance.Kick(characterName);

                    var log = new PenaltyLogDTO
                    {
                        AccountId = character.AccountId,
                        Reason = reason?.Trim(),
                        Penalty = isBanIp ? PenaltyType.IPBanned : PenaltyType.Banned,
                        DateStart = DateTime.Now,
                        DateEnd = duration == 0 ? DateTime.Now.AddYears(15) : DateTime.Now.AddDays(duration),
                        AdminName = Session.Character.Name,
                        IP = sessionTarget.CleanIpAddress
                    };
                    Character.InsertOrUpdatePenalty(log);
                    ServerManager.Instance.BannedCharacters.Add(character.CharacterId);
                    MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
                }
                else
                {
                    Session.SendPacket(Session.Character.GenerateSay("Target character must be online to ban him, so we can grab his IP.",
                        10));
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"),
                    10));
            }
        }

        public static async Task MuteMethod(this ClientSession Session, string characterName, string reason, int duration)
        {
            var characterToMute = DAOFactory.CharacterDAO.LoadByName(characterName);
            if (characterToMute != null)
            {
                var session = ServerManager.Instance.GetSessionByCharacterName(characterName);
                if (session?.Character.IsMuted() == false)
                    session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("MUTED_PLURAL"), reason, duration)));

                var log = new PenaltyLogDTO
                {
                    AccountId = characterToMute.AccountId,
                    Reason = reason,
                    Penalty = PenaltyType.Muted,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddMinutes(duration),
                    AdminName = Session.Character.Name
                };
                Character.InsertOrUpdatePenalty(log);
                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"),
                    10));
            }
        }

        public static void SendStats(this ClientSession Session, CharacterDTO characterDto)
        {
            Session.SendPacket(Session.Character.GenerateSay("----- CHARACTER -----", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Name: {characterDto.Name}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Id: {characterDto.CharacterId}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"State: {characterDto.State}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Gender: {characterDto.Gender}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Class: {characterDto.Class}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Level: {characterDto.Level}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"JobLevel: {characterDto.JobLevel}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"HeroLevel: {characterDto.HeroLevel}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Gold: {characterDto.Gold}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Bio: {characterDto.Biography}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapId: {characterDto.MapId}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapX: {characterDto.MapX}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"MapY: {characterDto.MapY}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Reputation: {characterDto.Reputation}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Dignity: {characterDto.Dignity}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Rage: {characterDto.RagePoint}", 13));
            Session.SendPacket(Session.Character.GenerateSay($"Compliment: {characterDto.Compliment}", 13));
            Session.SendPacket(Session.Character.GenerateSay(
                $"Faction: {(characterDto.Faction == FactionType.Demon ? Language.Instance.GetMessageFromKey("DEMON") : Language.Instance.GetMessageFromKey("ANGEL"))}",
                13));
            Session.SendPacket(Session.Character.GenerateSay("----- --------- -----", 13));
            var account = DAOFactory.AccountDAO.LoadById(characterDto.AccountId);
            if (account != null)
            {
                Session.SendPacket(Session.Character.GenerateSay("----- ACCOUNT -----", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Id: {account.AccountId}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Name: {account.Name}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Authority: {account.Authority}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"RegistrationIP: {account.RegistrationIP}", 13));
                Session.SendPacket(Session.Character.GenerateSay($"Email: {account.Email}", 13));
                Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
                IEnumerable<PenaltyLogDTO> penaltyLogs = ServerManager.Instance.PenaltyLogs
                    .Where(s => s.AccountId == account.AccountId).ToList();
                var penalty = penaltyLogs.LastOrDefault(s => s.DateEnd > DateTime.Now);
                Session.SendPacket(Session.Character.GenerateSay("----- PENALTY -----", 13));
                if (penalty != null)
                {
                    Session.SendPacket(Session.Character.GenerateSay($"Type: {penalty.Penalty}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"AdminName: {penalty.AdminName}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"Reason: {penalty.Reason}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"DateStart: {penalty.DateStart}", 13));
                    Session.SendPacket(Session.Character.GenerateSay($"DateEnd: {penalty.DateEnd}", 13));
                }

                Session.SendPacket(
                    Session.Character.GenerateSay($"Bans: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Banned)}",
                        13));
                Session.SendPacket(
                    Session.Character.GenerateSay($"Mutes: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Muted)}",
                        13));
                Session.SendPacket(
                    Session.Character.GenerateSay(
                        $"Warnings: {penaltyLogs.Count(s => s.Penalty == PenaltyType.Warning)}", 13));
                Session.SendPacket(Session.Character.GenerateSay("----- ------- -----", 13));
            }

            Session.SendPacket(Session.Character.GenerateSay("----- SESSION -----", 13));
            foreach (var connection in CommunicationServiceClient.Instance.RetrieveOnlineCharacters(characterDto
                .CharacterId))
                if (connection != null)
                {
                    var character = DAOFactory.CharacterDAO.LoadById(connection[0]);
                    if (character != null)
                    {
                        Session.SendPacket(Session.Character.GenerateSay($"Character Name: {character.Name}", 13));
                        Session.SendPacket(Session.Character.GenerateSay($"ChannelId: {connection[1]}", 13));
                        Session.SendPacket(Session.Character.GenerateSay("-----", 13));
                    }
                }

            Session.SendPacket(Session.Character.GenerateSay("----- ------------ -----", 13));
        }

        public static void AddMate(this ClientSession Session, short vnum, byte level, MateType mateType)
        {
            var mateNpc = ServerManager.GetNpcMonster(vnum);
            if (Session.CurrentMapInstance == Session.Character.Miniland && mateNpc != null)
            {
                level = Session.Character.Level;
                var mate = new Mate(Session.Character, mateNpc, level, mateType);
                Session.Character.AddPet(mate);
            }
            else
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_IN_MINILAND"), 0));
            }
        }
    }
}