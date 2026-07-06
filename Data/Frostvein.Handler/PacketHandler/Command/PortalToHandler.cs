using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
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