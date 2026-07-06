using Frostvein.Packets.Packets.FamilyCommandPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;

namespace Frostvein.Handler.PacketHandler.Family.Command
{
    public class FamilyTitleChangePacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyTitleChangePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void TitleChange(FamilyTitleChangePacket packet)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null
                                                 && Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
            {
                var packetsplit = packet.Data.Split(' ');
                if (packetsplit.Length != 4) return;
                if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                    return;
                }
                FamilyCharacterDTO fchar =
                    Session.Character.Family.FamilyCharacters.Find(s => s.Character.Name == packetsplit[2]);
                if (fchar != null && byte.TryParse(packetsplit[3], out var rank))
                {
                    fchar.Rank = (FamilyMemberRank)rank;

                    Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                        $"[Title][{Session.Character.Family.FamilyId}]CharacterName: {packetsplit[2]} Title: {fchar.Rank.ToString()}");

                    DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref fchar);
                    ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = Session.Character.Family.FamilyId,
                        SourceCharacterId = Session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = "fhis_stc",
                        Type = MessageType.Family
                    });
                    Session.SendPacket(Session.Character.GenerateFamilyMember());
                    Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                    Session.Character.LastFamilyAction = DateTime.Now;
                }
            }
        }

        #endregion
    }
}