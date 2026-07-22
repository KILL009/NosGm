using NosGm.Packets.Packets.FamilyCommandPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;

namespace NosGm.Handler.PacketHandler.Family.Command
{
    public class FamilyMessagePacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyMessagePacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyMessage(FamilyMessagePacket packet)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
                if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familydeputy
                    || Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familykeeper
                    && Session.Character.Family.ManagerCanShout
                    || Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                {
                    var msg = "";
                    var i = 0;
                    foreach (var str in packet.Data.Split(' '))
                    {
                        if (i >= 0) msg += str + " ";

                        i++;
                    }

                    Logger.LogUserEvent("GUILDCOMMAND", Session.GenerateIdentity(),
                        $"[FamilyMessage][{Session.Character.Family.FamilyId}]Message: {msg}");

                    Session.Character.Family.FamilyMessage = msg;
                    FamilyDTO fam = Session.Character.Family;
                    DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                    ServerManager.Instance.FamilyRefresh(Session.Character.Family.FamilyId);
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = Session.Character.Family.FamilyId,
                        SourceCharacterId = Session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = "fhis_stc",
                        Type = MessageType.Family
                    });
                    if (!string.IsNullOrWhiteSpace(msg))
                        CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                        {
                            DestinationCharacterId = Session.Character.Family.FamilyId,
                            SourceCharacterId = Session.Character.CharacterId,
                            SourceWorldId = ServerManager.Instance.WorldId,
                            Message = UserInterfaceHelper.GenerateInfo(
                                "--- Family Message ---\n" + Session.Character.Family.FamilyMessage),
                            Type = MessageType.Family
                        });
                }
        }

        #endregion
    }
}