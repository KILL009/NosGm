using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System.Linq;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class SudoHandler : IPacketHandler
    {
        #region Instantiation

        public SudoHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void SudoCommand(SudoPacket sudoPacket)
        {
            if (sudoPacket != null)
            {
                //Session.AddLogsCmd(sudoPacket);
                if (sudoPacket.CharacterName == "*")
                {
                    foreach (var sess in Session.CurrentMapInstance.Sessions.ToList()
                        .Where(s => s.Character?.Authority <= Session.Character.Authority))
                        sess.ReceivePacket(sudoPacket.CommandContents, true);
                }
                else
                {
                    var session = ServerManager.Instance.GetSessionByCharacterName(sudoPacket.CharacterName);

                    if (session != null && !string.IsNullOrWhiteSpace(sudoPacket.CommandContents))
                    {
                        if (session.Character?.Authority <= Session.Character.Authority)
                            session.ReceivePacket(sudoPacket.CommandContents, true);
                        else
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_DO_THAT"),
                                    0));
#pragma warning disable 4014

                    }
                    else
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"),
                                0));
                    }
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SudoPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}