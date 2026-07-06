using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Networking;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class HomeHandler : IPacketHandler
    {
        #region Instantiation

        public HomeHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Home(HomePacket homePacket)
        {
            if (homePacket != null)
            {
                //Session.AddLogsCmd(homePacket);
                ServerManager.Instance.ChangeMap(Session.Character.CharacterId, 1, 38, 112);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(HomePacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}