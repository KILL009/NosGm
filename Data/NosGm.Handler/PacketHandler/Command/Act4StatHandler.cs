using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;
using NosGm.GameObject.Extension.Message;

namespace NosGm.Handler.PacketHandler.Command
{
    public class Act4StatHandler : IPacketHandler
    {
        #region Instantiation

        public Act4StatHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task Act4Stat(Act4StatPacket packet)
        {
            if (packet != null && ServerManager.Instance.ChannelId == 51)
            {

                switch (packet.Faction)
                {
                    case 1:
                        ServerManager.Instance.Act4AngelStat.Percentage = packet.Value;
                        break;

                    case 2:
                        ServerManager.Instance.Act4DemonStat.Percentage = packet.Value;
                        break;
                }

                foreach (var sess in ServerManager.Instance.Sessions) sess.SendPacket(sess.Character.GenerateFc());
                MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(Act4StatPacket.ReturnHelp(), 10));
            }
        }

        #endregion
    }
}