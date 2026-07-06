using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Packets.Packets.ServerPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Family
{
    public class FamilyChatPacketHandler : IPacketHandler
    {
        #region Instantiation

        public FamilyChatPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void FamilyChat(FamilyChatPacket familyChatPacket)
        {
            if (string.IsNullOrEmpty(familyChatPacket.Message))
            {
                return;
            }

            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            {
                string msg = familyChatPacket.Message;
                string ccmsg = $"[{Session.Character.Name}]:{msg}";


                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = Session.Character.Family.FamilyId,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = ccmsg,
                    Type = MessageType.FamilyChat
                });
                Parallel.ForEach(ServerManager.Instance.Sessions.ToList(), session =>
                {
                    if (session.HasSelectedCharacter && session.Character.Family != null
                        && Session.Character.Family != null
                        && session.Character.Family?.FamilyId == Session.Character.Family?.FamilyId)
                    {
                        if (Session.HasCurrentMapInstance && session.HasCurrentMapInstance
                            && Session.CurrentMapInstance == session.CurrentMapInstance)
                        {
                            if (Session.Account.Authority != AuthorityType.GM && !Session.Character.InvisibleGm)
                            {
                                session.SendPacket(Session.Character.GenerateSay(msg, 6));
                            }
                            else
                            {
                                session.SendPacket(Session.Character.GenerateSay(ccmsg, 6, true));
                            }
                        }
                        else
                        {
                            session.SendPacket(Session.Character.GenerateSay(ccmsg, 6));
                        }

                        if (!Session.Character.InvisibleGm)
                        {
                            session.SendPacket(Session.Character.GenerateSpk(msg, 1));
                        }
                    }
                });
            }
        }

        #endregion
    }
}