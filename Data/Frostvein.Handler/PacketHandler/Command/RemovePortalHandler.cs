using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class RemovePortalHandler : IPacketHandler
    {
        #region Instantiation

        public RemovePortalHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void RemovePortal(RemovePortalPacket removePortalPacket)
        {
            if (Session.HasCurrentMapInstance)
            {
                var portal = Session.CurrentMapInstance.Portals.Find(s =>
                    s.SourceMapInstanceId == Session.Character.MapInstanceId && Map.GetDistance(
                        new MapCell { X = s.SourceX, Y = s.SourceY },
                        new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY }) < 10);
                if (portal != null)
                {
                    //Session.AddLogsCmd(removePortalPacket);
                    Session.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("NEAREST_PORTAL"), portal.SourceMapId,
                            portal.SourceX, portal.SourceY), 12));
                    portal.IsDisabled = true;
                    Session.CurrentMapInstance?.Broadcast(portal.GenerateGp());
                    Session.CurrentMapInstance?.Portals.Remove(portal);
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_PORTAL_FOUND"), 11));
                }
            }
        }

        #endregion
    }
}