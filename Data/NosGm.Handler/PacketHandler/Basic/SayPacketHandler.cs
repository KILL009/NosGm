using NosGm.Packets.Packets.ClientPackets;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Packets.Packets.ServerPackets;

using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Extension.Reputation;
using NosGm.GameObject.Extension.Translator;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace NosGm.Handler.PacketHandler.Basic
{
    public class SayPacketHandler : IPacketHandler
    {
        #region Instantiation

        public SayPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Say(NosGm.Packets.Packets.ClientPackets.SayPacket sayPacket)
        {
            Session.Character.SayRequests++;
            if (string.IsNullOrEmpty(sayPacket.Message))
            {
                return;
            }
            if (Session.Character.SayRequests > 30)
            {
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Auto ban SayRequests PL",
                    Penalty = PenaltyType.IPBanned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(20),
                    AdminName = "NosGm"
                };
                Character.InsertOrUpdatePenalty(log);
                Session?.Disconnect();
                return;
            }
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(x =>
            {
                if (Session?.Character?.SayRequests > 0)
                {
                    Session.Character.SayRequests = 0;
                }
            });
            var penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();
            var message = sayPacket.Message;
            if (Session.Character.IsMuted() && penalty != null)
            {
                if (Session.Character.Gender == GenderType.Female)
                {
                    var member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        var member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2)
                            .Replace(s =>
                                s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType)
                            .ToList().ForEach(o =>
                                o.Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1));
                    }

                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 11));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 12));
                }
                else
                {
                    var member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance &&
                        member != null)
                    {
                        var member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2)
                            .Replace(s =>
                                s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType)
                            .ToList().ForEach(o =>
                                o.Session.SendPacket(
                                    Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"),
                                        1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session,
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                    }

                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 11));
                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                            (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 12));
                }
            }
            else
            {

                var type = CharacterHelper.AuthorityChatColor(Session.Character.Authority);

                ConcurrentBag<ArenaTeamMember> member = null;
                lock (ServerManager.Instance.ArenaTeams)
                {
                    member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                }
                if (message.StartsWith("."))
                {
                    if (Session.Account.Language != null)
                    {
                        //MessageExtension.SendGrey(Session, $"Text: {message}\nFrom: {Session.Account.Language.ToUpper()} | To: EN\nResult:");
                        Task.Run(() => TranslatorExtension.TranslateChat(Session, message));
                    }
                    else
                    {
                        Session.SendPacket("info You did not set a Language\nPlease make sure to visit the Language NPC in NosVille");
                    }
                }
                else if (message.StartsWith("@Remove"))
                {
                    var charsToRemove = new string[] { "@Remove" };
                    foreach (var c in charsToRemove)
                    {
                        message = message.Replace(c, string.Empty);
                    }
                    if (Session.Character.SetStatus)
                    {
                        StatusExtension.RemoveStatus(Session);
                        MessageExtension.SendGreen(Session, "Status successfully removed");
                    }
                    else
                    {
                        MessageExtension.SendModal(Session, "You can not remove your Status\n\n\n\nReason: You did not set a Status");
                    }
                }
                else if (message.StartsWith("@"))
                {
                    var charsToRemove = new string[] { "@" };
                    foreach (var c in charsToRemove)
                    {
                        message = message.Replace(c, string.Empty);
                    }
                    Session.Character.SetStatus = true;
                    Session.Character.StatusMessage = message;
                    StatusExtension.GenerateStatus(Session, message);
                    MessageExtension.SendGrey(Session, $"Status successfully set to: '{message}'. To remove it, use @Remove");
                }
                else if (message.StartsWith("!"))
                {
                    var charsToRemove = new string[] { "!" };
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
                    {
                        foreach (var c in charsToRemove)
                        {
                            message = message.Replace(c, string.Empty);
                        }
                        string characterName = Session.Character.Name;
                        string tsId = Session.Character.Timespace.Info;
                        ServerManager.Instance.Broadcast(Session, $"say 1 {Session.Character.CharacterId} 13 [{characterName}][{tsId}]:" + $"{message}", ReceiverType.AllInTimeSpace);
                    }
                    else
                    {
                        MessageExtension.SendRed(Session, "This only works in TimeSpaces.");
                    }
                }
                else
                {
                    if (Session.Character.Authority >= AuthorityType.GM)
                    {
                        type = CharacterHelper.AuthorityChatColor(Session.Character.Authority);
                        if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                        {
                            var member2 = member.FirstOrDefault(o => o.Session == Session);
                            member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2)
                                .Replace(s =>
                                    s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType)
                                .ToList().ForEach(o =>
                                    o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), 1)));
                        }
                        else
                        {
                            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1), ReceiverType.AllExceptMe);
                        }
                        //AFTER HERE
                        message = $"[{Session.Character.Authority} {Session.Character.Name}]: " + message;
                    }

                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        var member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Where(s => s.ArenaTeamType == member2?.ArenaTeamType && s != member2).ToList().ForEach(o =>
                            o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), type,
                                Session.Account.Authority >= AuthorityType.GM)));
                    }
                    else if (ServerManager.Instance.ChannelId == 51 && Session.Account.Authority < AuthorityType.GM)
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), type), ReceiverType.AllExceptMeAct4);
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), type, false), ReceiverType.AllExceptMe);
                    }
                }
            }

            //LOGGER($"[{Session.Character.Name}]: {sayPacket.Message}");
        }

        #endregion
    }
}