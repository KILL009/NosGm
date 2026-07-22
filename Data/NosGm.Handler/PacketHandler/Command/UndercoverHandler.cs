using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;

namespace NosGm.Handler.PacketHandler.Command
{
    public class UndercoverHandler : IPacketHandler
    {
        #region Instantiation

        public UndercoverHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Undercover(UndercoverPacket undercoverPacket)
        {
            //Session.AddLogsCmd(undercoverPacket);
            Session.Character.Undercover = !Session.Character.Undercover;
            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                Session.CurrentMapInstance.MapInstanceId, Session.Character.PositionX, Session.Character.PositionY);
        }

        #endregion
    }
}