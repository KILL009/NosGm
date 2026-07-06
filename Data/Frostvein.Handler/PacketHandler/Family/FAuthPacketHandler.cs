using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;

namespace Frostvein.Handler.PacketHandler.Family
{
    public class FAuthPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FAuthPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void ChangeAuthority(FAuthPacket fAuthPacket)
        {
            if (Session.Character.Family == null || (Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head && Session.Character.FamilyCharacter.Authority != FamilyAuthority.Familydeputy))
            {
                return;
            }

            Session.Character.Family.InsertFamilyLog(FamilyLogType.RightChanged, Session.Character.Name,
                authority: fAuthPacket.MemberType, righttype: fAuthPacket.AuthorityId + 1,
                rightvalue: fAuthPacket.Value);
            switch (fAuthPacket.MemberType)
            {
                case FamilyAuthority.Familykeeper:
                    switch (fAuthPacket.AuthorityId)
                    {
                        case 0:
                            Session.Character.Family.ManagerCanInvite = fAuthPacket.Value == 1;
                            break;

                        case 1:
                            Session.Character.Family.ManagerCanNotice = fAuthPacket.Value == 1;
                            break;

                        case 2:
                            Session.Character.Family.ManagerCanShout = fAuthPacket.Value == 1;
                            break;

                        case 3:
                            Session.Character.Family.ManagerCanGetHistory = fAuthPacket.Value == 1;
                            break;

                        case 4:
                            Session.Character.Family.ManagerAuthorityType = (FamilyAuthorityType)fAuthPacket.Value;
                            break;
                    }

                    break;

                case FamilyAuthority.Member:
                    switch (fAuthPacket.AuthorityId)
                    {
                        case 0:
                            Session.Character.Family.MemberCanGetHistory = fAuthPacket.Value == 1;
                            break;

                        case 1:
                            Session.Character.Family.MemberAuthorityType = (FamilyAuthorityType)fAuthPacket.Value;
                            break;
                    }

                    break;
            }

            FamilyDTO fam = Session.Character.Family;
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
            Session.SendPacket(Session.Character.GenerateGInfo());
        }

        #endregion
    }
}