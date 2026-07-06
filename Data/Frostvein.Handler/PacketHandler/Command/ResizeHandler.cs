using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class ResizeHandler : IPacketHandler
    {
        #region Instantiation

        public ResizeHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Resize(ResizePacket resizePacket)
        {
            if (resizePacket != null)
            {
                //Session.AddLogsCmd(resizePacket);
                if (resizePacket.Value < 0) return;
                Session.Character.Size = resizePacket.Value;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateScal());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(ResizePacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}