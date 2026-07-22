using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Command
{
    public class ZoomHandler : IPacketHandler
    {
        #region Instantiation

        public ZoomHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Zoom(ZoomPacket zoomPacket)
        {
            if (zoomPacket != null)
            {
                Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(), $"[Zoom]Value: {zoomPacket.Value}");
                Session.SendPacket(UserInterfaceHelper.GenerateGuri(15, zoomPacket.Value, Session.Character.CharacterId));
            }
            else
            {
                Session.Character.GenerateSay(ZoomPacket.ReturnHelp(), 10);
            }
        }

        #endregion
    }
}