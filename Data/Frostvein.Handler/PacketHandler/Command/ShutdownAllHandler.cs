using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
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