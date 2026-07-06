using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class RestartHandler : IPacketHandler
    {
        #region Instantiation

        public RestartHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Restart(RestartPacket restartPacket)
        {
            var time = restartPacket.Time > 0 ? restartPacket.Time : 1;

            //Session.AddLogsCmd(restartPacket);
            if (ServerManager.Instance.TaskShutdown != null)
            {
                ServerManager.Instance.ShutdownStop = true;
                ServerManager.Instance.TaskShutdown = null;
            }
            else
            {
                ServerManager.Instance.IsReboot = true;
                ServerManager.Instance.TaskShutdown = ServerManager.Instance.ShutdownTaskAsync(time);
            }

            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}