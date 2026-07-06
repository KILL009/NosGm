using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class MinilandHandler : IPacketHandler
    {
        #region Instantiation

        public MinilandHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Miniland(MinilandPacket minilandPacket)
        {
            if (minilandPacket != null)
            {
                //Session.AddLogsCmd(minilandPacket);
                if (string.IsNullOrEmpty(minilandPacket.CharacterName))
                {
                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else
                {
                    var session = ServerManager.Instance.GetSessionByCharacterName(minilandPacket.CharacterName);
                    if (session != null)
                    {
                        ServerManager.Instance.JoinMiniland(Session, session);
                    }
                }
            }

            Session.Character.GenerateSay(MinilandPacket.ReturnHelp(), 10);
        }

        #endregion
    }
}