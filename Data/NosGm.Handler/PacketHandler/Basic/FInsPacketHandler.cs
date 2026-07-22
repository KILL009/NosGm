using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class FInsPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FInsPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void RelationAdd(FInsPacket fInsPacket)
        {
            var characterId = fInsPacket.CharacterId;

            if (Session.Character.CharacterId == characterId)
            {
                return;
            }

            var otherSession = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (otherSession != null)
            {
                if (Session.Character.Timespace != null || otherSession.Character.Timespace != null)
                {
                    return;
                }
                if (!Session.Character.IsFriendlistFull())
                {
                    if (!Session.Character.IsFriendOfCharacter(characterId) && (fInsPacket.Type == 1 || fInsPacket.Type == 2) || !Session.Character.IsMarried && (fInsPacket.Type == 34 || fInsPacket.Type == 69))
                    {
                        if (!Session.Character.IsBlockedByCharacter(characterId))
                        {
                            if (!Session.Character.IsBlockingCharacter(characterId))
                            {
                                if (otherSession.Character.MarryRequestCharacters.GetAllItems().Contains(Session.Character.CharacterId))
                                {
                                    switch (fInsPacket.Type)
                                    {
                                        case 34:
                                             Session.Character.DeleteRelation(characterId, CharacterRelationType.Friend);
                                             Session.Character.AddRelation(characterId, CharacterRelationType.Spouse);
                                             otherSession.SendPacket($"info {Language.Instance.GetMessageFromKey("MARRIAGE_ACCEPT")}");
                                             ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MARRIAGE_ACCEPT_SHOUT"), Session.Character.Name, otherSession.Character.Name), 0));
                                            break;

                                        case 69:
                                             otherSession.SendPacket($"info {Language.Instance.GetMessageFromKey("MARRIAGE_REJECTED")}");
                                             ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MARRIAGE_REJECT_SHOUT"), 1));
                                            break;
                                    }
                                }

                                if (otherSession.Character.FriendRequestCharacters.GetAllItems().Contains(Session.Character.CharacterId))
                                {
                                    switch (fInsPacket.Type)
                                    {
                                        case 1:
                                             Session.Character.AddRelation(characterId, CharacterRelationType.Friend);
                                             Session.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_ADDED")}");
                                             otherSession.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_ADDED")}");
                                            break;

                                        case 2:
                                             otherSession.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_REJECTED")}");
                                            otherSession.Character.FriendRequestCharacters.Clear();
                                            break;

                                        default:
                                            if (Session.Character.IsFriendlistFull())
                                            {
                                                 Session.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                                                 otherSession.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                                                otherSession.Character.FriendRequestCharacters.Clear();
                                            }

                                            break;
                                    }
                                }
                                else if (fInsPacket.Type != 34 && fInsPacket.Type != 69)
                                {
                                    if (otherSession.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                    {
                                        return;
                                    }

                                    if (otherSession.Character.FriendRequestBlocked)
                                    {
                                         Session.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_REJECTED")}");
                                        return;
                                    }

                                     otherSession.SendPacket(UserInterfaceHelper.GenerateDialog($"#fins^1^{Session.Character.CharacterId} #fins^2^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("FRIEND_ADD"), Session.Character.Name)}"));
                                    Session.Character.FriendRequestCharacters.Add(characterId);
                                }
                            }
                            else
                            {
                                 Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKING")}");
                            }
                        }
                        else
                        {
                             Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")}");
                        }
                    }
                    else
                    {
                         Session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_FRIEND")}");
                    }
                }
                else
                {
                     Session.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                }
            }
        }

        #endregion
    }
}