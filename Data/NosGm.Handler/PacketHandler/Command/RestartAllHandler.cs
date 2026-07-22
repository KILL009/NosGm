using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Command
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