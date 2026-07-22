using NosGm.Packets.Packets.FamilyCommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Family.Command
{
    public class FamilyTodayPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyTodayPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void TodayMessage(FamilyTodayPacket packet)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            {
                if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                    return;
                }
                var msg = "";
                var i = 0;
                foreach (var str in packet.Data.Split(' '))
                {
                    if (i >= 0) msg += str + " ";

                    i++;
                }

                Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                    $"[Today][{Session.Character.Family.FamilyId}]CharacterName: {Session.Character.Name} Title: {msg}");

                var islog = Session.Character.Family.FamilyLogs.Any(s =>
                    s.FamilyLogType == FamilyLogType.DailyMessage
                    && s.FamilyLogData.StartsWith(Session.Character.Name, StringComparison.CurrentCulture)
                    && s.Timestamp.AddDays(1) > DateTime.Now);
                if (!islog)
                {
                    Session.Character.FamilyCharacter.DailyMessage = msg;
                    var fchar = Session.Character.FamilyCharacter;
                    DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref fchar);
                    Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                    Session.Character.Family.InsertFamilyLog(FamilyLogType.DailyMessage, Session.Character.Name,
                        message: msg);
                }
                else
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_CHANGE_MESSAGE")));
                }
                Session.Character.LastFamilyAction = DateTime.Now;
            }
        }

        #endregion
    }
}