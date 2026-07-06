using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Event.Handler
{
    public static class OnLoadEvent
    {
        public static ConfigurationObject Configuration { get; set; }

        public static ThreadSafeSortedList<long, Family> FamilyList { get; set; }

        public static IEnumerable<ClientSession> Sessions =>
            _sessions.Where(s => s.HasSelectedCharacter && !s.IsDisposing && s.IsConnected);

        /// <summary>
        ///     List of all connected clients.
        /// </summary>
        private static readonly ThreadSafeSortedList<long, ClientSession> _sessions;

        public static ClientSession GetSessionByCharacterId(long characterId) => _sessions.ContainsKey(characterId) ? _sessions[characterId] : null;

        public static Guid WorldId { get; private set; }

        public static void Shout(string message, bool noAdminTag = false)
        {
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateSay(
                (noAdminTag ? "" : $"({Language.Instance.GetMessageFromKey("ADMINISTRATOR")})") + message, 10));
            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(message, 2));
        }


        // await Task.Run(() =>
        public static async Task OnConfigurationEvent(object sender, EventArgs e)
        {
            Configuration = (ConfigurationObject)sender;
        }

        public static async Task OnFamilyRefresh(object sender, EventArgs e)
        {
            var tuple = (Tuple<long, bool>)sender;
            var familyId = tuple.Item1;
            var famdto = DAOFactory.FamilyDAO.LoadById(familyId);
            var fam = FamilyList[familyId];
            lock (FamilyList)
            {
                if (famdto != null)
                {
                    var newFam = new Family(famdto);
                    if (fam != null)
                    {
                        newFam.FamilyRoom = fam.FamilyRoom;
                        newFam.LandOfDeath = fam.LandOfDeath;
                        newFam.FamilyTower = fam.FamilyTower;
                        newFam.Act4Raid = fam.Act4Raid;
                        newFam.Act4RaidBossMap = fam.Act4RaidBossMap;
                        newFam.NewEvent = fam.NewEvent;
                    }

                    newFam.FamilyCharacters = new List<FamilyCharacter>();
                    foreach (var famchar in DAOFactory.FamilyCharacterDAO.LoadByFamilyId(famdto.FamilyId)
                        .ToList())
                    {
                        newFam.FamilyCharacters.Add(new FamilyCharacter(famchar));
                    }
                    foreach (FamilySkillMissionDTO famskill in DAOFactory.FamilySkillMissionDAO.LoadByFamilyId(famdto.FamilyId).ToList())
                    {
                        newFam.FamilySkillMissions.Add(new FamilySkillMission(famskill));
                    }

                    var familyHead = newFam.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                    if (familyHead != null)
                    {
                        newFam.Warehouse = new Inventory(new Character(familyHead.Character));
                        foreach (var inventory in DAOFactory.ItemInstanceDAO
                            .LoadByCharacterId(familyHead.CharacterId)
                            .Where(s => s.Type == InventoryType.FamilyWareHouse).ToList())
                        {
                            inventory.CharacterId = familyHead.CharacterId;
                            newFam.Warehouse[inventory.Id] = new ItemInstance(inventory);
                        }
                    }

                    newFam.FamilyLogs = DAOFactory.FamilyLogDAO.LoadByFamilyId(famdto.FamilyId).ToList();
                    FamilyList[familyId] = newFam;

                    foreach (var session in Sessions.Where(s =>
                        newFam.FamilyCharacters.Any(m => m.CharacterId == s.Character.CharacterId)))
                    {
                        if (session.Character.LastFamilyLeave < DateTime.Now.AddDays(-1).Ticks)
                        {
                            session.Character.Family = newFam;

                            if (tuple.Item2)
                            {
                                session.Character.ChangeFaction((FactionType)newFam.FamilyFaction);
                            }
                            session?.CurrentMapInstance?.Broadcast(session?.Character?.GenerateGidx());
                        }
                        session.Character.Family = newFam;

                        if (tuple.Item2)
                        {
                            session.Character.ChangeFaction((FactionType)newFam.FamilyFaction);
                        }

                        session?.CurrentMapInstance?.Broadcast(session?.Character?.GenerateGidx());
                        session?.SendPacket(FamilySystemExtensions.GenerateFmi(session));
                        session?.SendPacket(FamilySystemExtensions.GenerateFmp(session));
                    }
                }
                else if (fam != null)
                {
                    lock (FamilyList)
                    {
                        FamilyList.Remove(fam.FamilyId);
                    }

                    foreach (var sess in Sessions.Where(s =>
                        fam.FamilyCharacters.Any(f => f.CharacterId.Equals(s.Character.CharacterId))))
                    {
                        sess.Character.Family = null;
                        sess.SendPacket(sess.Character.GenerateGidx());
                        sess?.SendPacket(FamilySystemExtensions.GenerateFmi(sess));
                        sess?.SendPacket(FamilySystemExtensions.GenerateFmp(sess));

                    }
                }
            }
        }

        public static async Task OnMailSent(object sender, EventArgs e)
        {
            var mail = (MailDTO)sender;

            var session = GetSessionByCharacterId(mail.IsSenderCopy ? mail.SenderId : mail.ReceiverId);
            if (session != null)
            {
                if (mail.AttachmentVNum != null)
                {
                    session.Character.MailList.Add(
                        (session.Character.MailList.Count > 0
                            ? session.Character.MailList.OrderBy(s => s.Key).Last().Key
                            : 0) + 1, mail);
                    session.SendPacket(session.Character.GenerateParcel(mail));

                    //session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"), GetItem(mail.AttachmentVNum.Value)?.Name, mail.AttachmentAmount), 12));
                }
                else
                {
                    session.Character.MailList.Add(
                        (session.Character.MailList.Count > 0
                            ? session.Character.MailList.OrderBy(s => s.Key).Last().Key
                            : 0) + 1, mail);
                    session.SendPacket(session.Character.GeneratePost(mail,
                        mail.IsSenderCopy ? (byte)2 : (byte)1));
                }
            }
        }

        public static async Task OnMessageSentToCharacter(object sender, EventArgs e)
        {
            if (sender != null)
            {
                var message = (SCSCharacterMessage)sender;

                var targetSession = Sessions.SingleOrDefault(s =>
                    s.Character.CharacterId == message.DestinationCharacterId);
                switch (message.Type)
                {
                    case MessageType.WhisperGM:
                    case MessageType.Whisper:
                        if (targetSession == null)
                        {
                            return;
                        }

                        if (targetSession.Character.GmPvtBlock)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(
                                        new SCSCharacterMessage
                                        {
                                            DestinationCharacterId = message.SourceCharacterId,
                                            SourceCharacterId = message.DestinationCharacterId.Value,
                                            SourceWorldId = WorldId,
                                            Message = targetSession.Character.GenerateSay(
                                                        Language.Instance.GetMessageFromKey("GM_CHAT_BLOCKED"), 10),
                                            Type = MessageType.Other
                                        });
                            }
                        }
                        else if (targetSession.Character.WhisperBlocked && DAOFactory.AccountDAO.LoadById(DAOFactory.CharacterDAO.LoadById(message.SourceCharacterId).AccountId).Authority < AuthorityType.GM)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(
                                        new SCSCharacterMessage
                                        {
                                            DestinationCharacterId = message.SourceCharacterId,
                                            SourceCharacterId = message.DestinationCharacterId.Value,
                                            SourceWorldId = WorldId,
                                            Message = UserInterfaceHelper.GenerateMsg(
                                                        Language.Instance.GetMessageFromKey("USER_WHISPER_BLOCKED"), 0),
                                            Type = MessageType.Other
                                        });
                            }
                        }
                        else
                        {
                            if (message.SourceWorldId != WorldId)
                            {
                                if (message.DestinationCharacterId != null)
                                {
                                    CommunicationServiceClient.Instance.SendMessageToCharacter(
                                            new SCSCharacterMessage
                                            {
                                                DestinationCharacterId = message.SourceCharacterId,
                                                SourceCharacterId = message.DestinationCharacterId.Value,
                                                SourceWorldId = WorldId,
                                                Message = targetSession.Character.GenerateSay(
                                                            string.Format(
                                                                    Language.Instance.GetMessageFromKey(
                                                                            "MESSAGE_SENT_TO_CHARACTER"),
                                                                    targetSession.Character.Name, ServerManager.Instance.ChannelId), 11),
                                                Type = MessageType.Other
                                            });
                                }

                                targetSession.SendPacket(
                                    $"{message.Message} <{Language.Instance.GetMessageFromKey("CHANNEL")}: {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)}>");
                            }
                            else
                            {
                                targetSession.SendPacket(message.Message);
                            }
                        }

                        break;

                    case MessageType.Shout:
                        Shout(message.Message);
                        break;

                    case MessageType.PrivateChat:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.FamilyChat:
                        if (message.DestinationCharacterId.HasValue && message.SourceWorldId != WorldId)
                        {
                            foreach (var session in ServerManager.Instance.Sessions)
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null &&
                                    session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket($"sayi2 1 -1 6 1081 20 {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)} {message.Name} {message.Message}");
                                }
                            }
                        }

                        break;

                    case MessageType.Family:
                        if (message.DestinationCharacterId.HasValue)
                        {
                            foreach (var session in ServerManager.Instance.Sessions)
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null &&
                                    session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket(message.Message);
                                }
                            }
                        }

                        break;

                    case MessageType.Other:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.Broadcast:
                        foreach (var session in ServerManager.Instance.Sessions)
                        {
                            session.SendPacket(message.Message);
                        }

                        break;

                    case MessageType.UpdateExploit:
                        if (!message.DestinationCharacterId.HasValue)
                        {
                            return;
                        }

                        var target = Sessions.FirstOrDefault(s =>
                            s.Character?.CharacterId == message.DestinationCharacterId.Value);

                        if (target == null || !target.HasSelectedCharacter)
                        {
                            return;
                        }

                        var split = message.Message.Split(' ');

                        if (split.Length != 2)
                        {
                            return;
                        }

                        var exploitType = (CharacterExploitType)Enum.Parse(typeof(CharacterExploitType), split[0]);
                        var value = long.Parse(split[1]);

                        var exploit =
                            target.Character.Exploit.FirstOrDefault(s => s.CharacterExploitType == exploitType);

                        if (exploit == null)
                        {
                            return;
                        }

                        exploit.Stat = value;
                        target.SendPacket(target.Character.GenerateSay("Exploit restored", 12));
                        break;
                }
            }
        }

        public static List<PenaltyLogDTO> PenaltyLogs { get; set; }

        public static async Task OnPenaltyLogRefresh(object sender, EventArgs e)
        {
            var relId = (int)sender;
            var reldto = DAOFactory.PenaltyLogDAO.LoadById(relId);
            var rel = PenaltyLogs.Find(s => s.PenaltyLogId == relId);
            if (reldto != null)
            {
                if (rel != null)
                {
                }
                else
                {
                    PenaltyLogs.Add(reldto);
                }
            }
            else if (rel != null)
            {
                PenaltyLogs.Remove(rel);
            }
        }

        private static bool _inRelationRefreshMode;
        public static List<CharacterRelationDTO> CharacterRelations { get; set; }
        public static async Task OnRelationRefresh(object sender, EventArgs e)
        {
            _inRelationRefreshMode = true;
            var relId = (long)sender;
            lock (CharacterRelations)
            {
                var reldto = DAOFactory.CharacterRelationDAO.LoadById(relId);
                var rel = CharacterRelations.Find(s => s.CharacterRelationId == relId);
                if (reldto != null)
                {
                    if (rel != null)
                    {
                        CharacterRelations.Find(s => s.CharacterRelationId == rel.CharacterRelationId)
                                          .RelationType = reldto.RelationType;
                    }
                    else
                    {
                        CharacterRelations.Add(reldto);
                    }
                }
                else if (rel != null)
                {
                    CharacterRelations.Remove(rel);
                }
            }

            _inRelationRefreshMode = false;
        }

        public static async Task OnSessionKicked(object sender, EventArgs e)
        {
            if (sender != null)
            {
                var kickedSession = (Tuple<long?, long?>)sender;
                if (!kickedSession.Item1.HasValue && !kickedSession.Item2.HasValue)
                {
                    return;
                }

                var accId = kickedSession.Item1;
                var sessId = kickedSession.Item2;

                var targetSession = ServerManager.Instance.CharacterScreenSessions.FirstOrDefault(s =>
                    s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
                targetSession = Sessions.FirstOrDefault(s =>
                    s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
            }
        }

        public static async Task OnStaticBonusRefresh(object sender, EventArgs e)
        {
            var characterId = (long)sender;

            var sess = GetSessionByCharacterId(characterId);
            if (sess != null)
            {
                sess.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(characterId).ToList();
            }
        }
    }
}
