using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;
using System.Reactive.Linq;

namespace Frostvein.Handler.PacketHandler.Family
{
    public class JoinFamilyPacketHandler : IPacketHandler
    {
        #region Instantiation

        public JoinFamilyPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void JoinFamily(JoinFamilyPacket joinFamilyPacket)
        {
            long characterId = joinFamilyPacket.CharacterId;

            if (joinFamilyPacket.Type == 1)
            {
                if (Session.Character.Family != null)
                {
                    return;
                }
                if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                    return;
                }

                ClientSession inviteSession = ServerManager.Instance.GetSessionByCharacterId(characterId);

                if (inviteSession?.Character.FamilyInviteCharacters.GetAllItems().Contains(Session.Character.CharacterId) == true
                    && inviteSession.Character.Family != null
                    && inviteSession.Character.Family.FamilyCharacters != null)
                {
                    if (inviteSession.Character.Family.FamilyCharacters.Count + 1 > inviteSession.Character.Family.MaxSize)
                    {
                        return;
                    }

                    FamilyCharacterDTO familyCharacter = new FamilyCharacterDTO
                    {
                        CharacterId = Session.Character.CharacterId,
                        DailyMessage = "",
                        Experience = 0,
                        Authority = FamilyAuthority.Member,
                        FamilyId = inviteSession.Character.Family.FamilyId,
                        Rank = 0
                    };

                    DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref familyCharacter);

                    inviteSession.Character.Family.InsertFamilyLog(FamilyLogType.UserManaged,
                        inviteSession.Character.Name, Session.Character.Name);

                    Logger.LogUserEvent("GUILDJOIN", Session.GenerateIdentity(),
                        $"[FamilyJoin][{inviteSession.Character.Family.FamilyId}]");

                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = inviteSession.Character.Family.FamilyId,
                        SourceCharacterId = Session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("FAMILY_JOINED"), Session.Character.Name,
                                inviteSession.Character.Family.Name), 0),
                        Type = MessageType.Family
                    });

                    long familyId = inviteSession.Character.Family.FamilyId;

                    Session.Character.Family = inviteSession.Character.Family;
                    Session.Character.ChangeFaction((FactionType)inviteSession.Character.Family.FamilyFaction);
                    Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                    ServerManager.Instance.FamilyRefresh(familyId));
                    Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(o =>
                    ServerManager.Instance.FamilyRefresh(familyId));

                    Session.SendPacket(Session.Character.GenerateFamilyMember());
                    Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                    Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
                    Session.Character.LastFamilyAction = DateTime.Now;
                }
            }
        }

        #endregion
    }
}