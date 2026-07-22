using NosGm.Extension.Extension.Command;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
{
    public class PortalToHandler : IPacketHandler
    {
        #region Instantiation

        public PortalToHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void PortalTo(PortalToPacket portalToPacket)
        {
            if (portalToPacket != null)
            {
                //Session.AddLogsCmd(portalToPacket);
                Session.AddPortal(portalToPacket.DestinationMapId, portalToPacket.DestinationX,
                    portalToPacket.DestinationY,
                    portalToPacket.PortalType == null ? (short)-1 : (short)portalToPacket.PortalType, false);
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(PortalToPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}