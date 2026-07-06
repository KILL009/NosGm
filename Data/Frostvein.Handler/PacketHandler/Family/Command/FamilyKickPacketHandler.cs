using Frostvein.Packets.Packets.FamilyCommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Frostvein.Handler.PacketHandler.Family.Command
{
    public class FamilyKickPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyKickPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyKick(FamilyKickPacket packet)
        {
            if (packet == null) return;

            if (Session.Character.Family?.FamilyCharacters == null || Session.Character.FamilyCharacter == null) return;

            if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                return;
            }

            if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Member
                || Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familykeeper)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(
                        string.Format(Language.Instance.GetMessageFromKey("NOT_ALLOWED_KICK"))));
                return;
            }



            var characterName = packet.Name;

            Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                $"[FamilyKick][{Session.Character.Family.FamilyId}]CharacterName: {characterName}");

            var familyCharacter =
                Session.Character.Family.FamilyCharacters.FirstOrDefault(s => s.Character.Name == characterName);

            if (familyCharacter?.FamilyId != Session.Character.Family.FamilyId) return;

            if (familyCharacter.Authority == FamilyAuthority.Head)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_KICK_HEAD")));
                return;
            }

            if (familyCharacter.CharacterId == Session.Character.CharacterId)
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_KICK_YOURSELF")));
                return;
            }

            var kickSession = ServerManager.Instance.GetSessionByCharacterId(familyCharacter.CharacterId);

            if (kickSession != null)
            {
                DAOFactory.FamilyCharacterDAO.Delete(familyCharacter.CharacterId);

                Session.Character.Family.InsertFamilyLog(FamilyLogType.FamilyManaged, familyCharacter.Character.Name);

                kickSession.Character.Family = null;
                kickSession.Character.LastFamilyLeave = DateTime.Now.Ticks;

                Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o =>
                {
                    if (Session?.Character == null)
                    {
                        return;
                    }

                    ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                });
            }
            else
            {
                if (CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup,
                    familyCharacter.CharacterId))
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        Language.Instance.GetMessageFromKey("CANT_KICK_PLAYER_ONLINE_OTHER_CHANNEL")));
                    return;
                }

                DAOFactory.FamilyCharacterDAO.Delete(familyCharacter.CharacterId);

                Session.Character.Family.InsertFamilyLog(FamilyLogType.FamilyManaged, familyCharacter.Character.Name);

                var familyCharacterDTO = familyCharacter.Character;

                familyCharacterDTO.LastFamilyLeave = DateTime.Now.Ticks;

                DAOFactory.CharacterDAO.InsertOrUpdate(familyCharacterDTO);
                Session.Character.LastFamilyAction = DateTime.Now;
            }
        }

        #endregion
    }
}