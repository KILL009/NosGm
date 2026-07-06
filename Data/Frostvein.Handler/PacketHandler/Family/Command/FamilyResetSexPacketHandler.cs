using Frostvein.Packets.Packets.FamilyCommandPackets;
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

namespace Frostvein.Handler.PacketHandler.Family.Command
{
    public class FamilyResetSexPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyResetSexPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ResetSex(FamilyResetSexPacket packet)
        {
            var packetsplit = packet.Data.Split(' ');
            if (packetsplit.Length != 3) return;
            if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
            {
                Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                return;
            }
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null
                                                 && Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head
                                                 && byte.TryParse(packetsplit[2], out var rank))
            {
                foreach (var familyCharacter in Session.Character.Family.FamilyCharacters)
                {
                    FamilyCharacterDTO familyCharacterDto = familyCharacter;
                    familyCharacterDto.Rank = 0;
                    DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref familyCharacterDto);
                }

                Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                    $"[Sex][{Session.Character.Family.FamilyId}]");

                FamilyDTO fam = Session.Character.Family;
                fam.FamilyHeadGender = (GenderType)rank;
                DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = fam.FamilyId,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = "fhis_stc",
                    Type = MessageType.Family
                });
                Session.SendPacket(Session.Character.GenerateFamilyMember());
                Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());

                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = fam.FamilyId,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("FAMILY_HEAD_CHANGE_GENDER")), 0),
                    Type = MessageType.Family
                });
                Session.Character.LastFamilyAction = DateTime.Now;
            }
        }

        #endregion
    }
}