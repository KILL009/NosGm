using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ShutdownAllHandler : IPacketHandler
    {
        #region Instantiation

        public ShutdownAllHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ShutdownAll(ShutdownAllPacket shutdownAllPacket)
        {
            if (shutdownAllPacket != null)
            {
                //Session.AddLogsCmd(shutdownAllPacket);
                if (!string.IsNullOrEmpty(shutdownAllPacket.WorldGroup))
                    CommunicationServiceClient.Instance.Shutdown(shutdownAllPacket.WorldGroup);
                else
                    CommunicationServiceClient.Instance.Shutdown(ServerManager.Instance.ServerGroup);

                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ShutdownAllPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}