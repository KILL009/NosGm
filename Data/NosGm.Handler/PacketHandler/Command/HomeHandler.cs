using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
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