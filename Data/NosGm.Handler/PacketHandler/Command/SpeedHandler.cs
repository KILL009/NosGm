using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;

namespace NosGm.Handler.PacketHandler.Command
{
    public class SpeedHandler : IPacketHandler
    {
        #region Instantiation

        public SpeedHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Speed(SpeedPacket speedPacket)
        {
            if (speedPacket != null)
            {
                //Session.AddLogsCmd(speedPacket);
                if (speedPacket.Value < 60)
                {
                    Session.Character.Speed = speedPacket.Value;
                    Session.Character.IsCustomSpeed = true;
                    Session.SendPacket(Session.Character.GenerateCond());
                }

                if (speedPacket.Value == 0)
                {
                    Session.Character.IsCustomSpeed = false;
                    Session.Character.LoadSpeed();
                    Session.SendPacket(Session.Character.GenerateCond());
                }
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(SpeedPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}