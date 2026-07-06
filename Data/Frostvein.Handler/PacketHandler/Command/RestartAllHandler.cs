using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Client;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class RestartAllHandler : IPacketHandler
    {
        #region Instantiation

        public RestartAllHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task RestartAll(RestartAllPacket restartAllPacket)
        {
            if (restartAllPacket != null)
            {
                //Session.AddLogsCmd(restartAllPacket);
                var worldGroup = !string.IsNullOrEmpty(restartAllPacket.WorldGroup)
                    ? restartAllPacket.WorldGroup
                    : ServerManager.Instance.ServerGroup;

                var time = restartAllPacket.Time;

                if (time < 1) time = 5;

                CommunicationServiceClient.Instance.Restart(worldGroup, time);

                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(RestartAllPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}