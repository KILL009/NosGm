using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class KickHandler : IPacketHandler
    {
        #region Instantiation

        public KickHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Kick(KickPacket kickPacket)
        {
            if (kickPacket != null)
            {
                //Session.AddLogsCmd(kickPacket);
                if (kickPacket.CharacterName == "*")
                    foreach (var session in ServerManager.Instance.Sessions)
                        session.Disconnect();

                ServerManager.Instance.Kick(kickPacket.CharacterName);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(KickPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}