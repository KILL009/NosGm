using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.Handler.PacketHandler.Command
{
    public class GuriHandler : IPacketHandler
    {
        #region Instantiation

        public GuriHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Guri(GuriCommandPacket guriCommandPacket)
        {
            if (guriCommandPacket != null)
            {
                //Session.AddLogsCmd(guriCommandPacket);
                Session.SendPacket(UserInterfaceHelper.GenerateGuri(guriCommandPacket.Type, guriCommandPacket.Argument,
                    Session.Character.CharacterId, guriCommandPacket.Value));
            }

            Session.Character.GenerateSay(GuriCommandPacket.ReturnHelp(), 10);
        }

        #endregion
    }
}