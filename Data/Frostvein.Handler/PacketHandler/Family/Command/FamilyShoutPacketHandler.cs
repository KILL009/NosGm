using Frostvein.Packets.Packets.FamilyCommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System;

namespace Frostvein.Handler.PacketHandler.Family.Command
{
    public class FamilyShoutPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyShoutPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyCall(FamilyShoutPacket packet)
        {
            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            {
                if (Session.Character.LastFamilyAction.AddSeconds(120) > DateTime.Now)
                {
                    Session.SendPacket("info You have to wait 2 Minutes before doing that again");
                    return;
                }
                if (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familydeputy
                    || (Session.Character.FamilyCharacter.Authority == FamilyAuthority.Familykeeper
                        && Session.Character.Family.ManagerCanShout)
                    || Session.Character.FamilyCharacter.Authority == FamilyAuthority.Head)
                {
                    Logger.Log.Info("GUILDCOMMAND " + Session.GenerateIdentity() + $" [FamilyShout][{Session.Character.Family.FamilyId}]Message: {packet.Message}");

                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = Session.Character.Family.FamilyId,
                        SourceCharacterId = Session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg(
                            $"<{Language.Instance.GetMessageFromKey("FAMILYCALL")}{Session.Character.Name}:> {packet.Message}", 0),
                        Type = MessageType.Family
                    });
                    Session.Character.LastFamilyAction = DateTime.Now;
                }
            }
        }

        #endregion
    }
}