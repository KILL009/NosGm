using Frostvein.Packets.Packets.FamilyCommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.Handler.PacketHandler.Family.Command
{
    public class FamilyInvitePacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyInvitePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void InviteFamily(FamilyInvitePacket packet)
        {
            if (packet == null)
            {
                return;
            }

            var name = packet.Name;

            if (Session.Character.LastFamilyAction.AddSeconds(5) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 5 seconds before doing that again");
                return;
            }

            if (Session.Character.Family == null || Session.Character.FamilyCharacter == null)
            {
                return;
            }

            if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Member
                || (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familykeeper
                 && !Session.Character.Family.ManagerCanInvite))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("FAMILY_INVITATION_NOT_ALLOWED"))));
                return;
            }

            Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                $"[FamilyInvite][{Session.Character.Family.FamilyId}]Message: {name}");
            var otherSession = ServerManager.Instance.GetSessionByCharacterName(name);
            if (otherSession == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"))));
                return;
            }

            if (otherSession.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
            {
                return;
            }

            if (otherSession.Character.FamilyRequestBlocked)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAMILY_BLOCKED"), 0));
                return;
            }
                
            if (Session.Character.IsBlockedByCharacter(otherSession.Character.CharacterId))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                return;
            }

            if (Session.Character.Family.FamilyCharacters.Count + 1 > Session.Character.Family.MaxSize)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("FAMILY_FULL")));
                return;
            }

            if (otherSession.Character.Family != null || otherSession.Character.FamilyCharacter != null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ALREADY_IN_FAMILY")));
                return;
            }

            if (ServerManager.Instance.ChannelId == 51 && otherSession.Character.Faction != Session.Character.Faction)
            {
                return;
            }

            Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("FAMILY_INVITED"), otherSession.Character.Name)));
            otherSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                $"#gjoin^1^{Session.Character.CharacterId} #gjoin^2^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("ASK_FAMILY_INVITED"), Session.Character.Family.Name, Session.Character.Family.FamilyLevel)}"));
            Session.Character.FamilyInviteCharacters.Add(otherSession.Character.CharacterId);
            Session.Character.LastFamilyAction = DateTime.Now;
        }

        #endregion
    }
}