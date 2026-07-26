using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.BasicPacket.CharScreen
{
    public class DeleteCharacterPacketHandler : IPacketHandler
    {
        #region Instantiation

        public DeleteCharacterPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        private ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task DeleteCharacterAsync(CharacterDeletePacket characterDeletePacket)
        {
            GeneralLogDTO lastDelete = DAOFactory.GeneralLogDAO.LoadLatestByAccountAndType(
                Session.Account.AccountId,
                "Deleted");

            if (lastDelete?.Timestamp.AddMinutes(5) > DateTime.Now)
            {
                return;
            }
            if (Session.HasCurrentMapInstance)
            {
                return;
            }

            if (characterDeletePacket.Password == null)
            {
                Session.SendPacket($"say 1 0 10 Fixxed, thanks for try.");
                return;
            }

            Logger.LogUserEvent("DELETECHARACTER", Session.GenerateIdentity(), $"[DeleteCharacter]Name: {characterDeletePacket.Slot}");
            var account = DAOFactory.AccountDAO.LoadById(Session.Account.AccountId);
            if (account == null)
            {
                return;
            }

            if (PasswordHashService.VerifyPassword(
                    account.Password,
                    characterDeletePacket.Password,
                    true,
                    out _))
            {
                var character = DAOFactory.CharacterDAO.LoadBySlot(account.AccountId, characterDeletePacket.Slot);
                if (character == null)
                {
                    return;
                }

                // Remove all relations from deleted character
                var relationshipList = ServerManager.Instance.CharacterRelations.Where(s => s.CharacterId == character.CharacterId || s.RelatedCharacterId == character.CharacterId).ToList();

                foreach (var relation in relationshipList)
                {
                    await DeleteRelation(character.CharacterId, relationshipList, relation.RelatedCharacterId, relation.RelationType);
                }

                //DAOFactory.GeneralLogDAO.SetCharIdNull(Convert.ToInt64(character.CharacterId));
                DAOFactory.CharacterDAO.DeleteByPrimaryKey(account.AccountId, characterDeletePacket.Slot);
                new EntryPointPacketHandler(Session).LoadCharacters(new NosGmEntryPointPacket
                { PacketData = string.Empty });

                DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    IpAddress = Session.IpAddress,
                    LogData = "Character Deleted",
                    LogType = "Deleted",
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BAD_PASSWORD")}");
            }
        }

        private static async Task DeleteRelation(long mainCharacterId, List<CharacterRelationDTO> relations, long characterId, CharacterRelationType relationType)
        {
            CharacterRelationDTO chara = relations.Find(s =>
                (s.RelatedCharacterId == characterId || s.CharacterId == characterId) && s.RelationType == relationType);
            if (chara != null)
            {
                long id = chara.CharacterRelationId;
                CharacterDTO charac = DAOFactory.CharacterDAO.LoadById(characterId);
                DAOFactory.CharacterRelationDAO.Delete(id);
                ServerManager.Instance.RelationRefresh(id);

                if (charac != null)
                {
                    List<CharacterRelationDTO> lst = ServerManager.Instance.CharacterRelations.Where(s => s.CharacterId == characterId || s.RelatedCharacterId == characterId).ToList();
                    string result = "finit";

                    foreach (CharacterRelationDTO relation in lst.Where(c => c.RelationType == CharacterRelationType.Friend || c.RelationType == CharacterRelationType.Spouse))
                    {
                        long id2 = relation.RelatedCharacterId == charac.CharacterId ? relation.CharacterId : relation.RelatedCharacterId;
                        bool isOnline = CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup, id2);
                        result += $" {id2}|{(short)relation.RelationType}|{(isOnline ? 1 : 0)}|{DAOFactory.CharacterDAO.LoadById(id2).Name}";
                    }

                    int? sentChannelId = CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = charac.CharacterId,
                        SourceCharacterId = mainCharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = result,
                        Type = MessageType.PrivateChat
                    });
                }
            }
        }

        #endregion
    }
}
