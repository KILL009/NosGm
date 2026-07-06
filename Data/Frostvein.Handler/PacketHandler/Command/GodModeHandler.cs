using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class GodModeHandler : IPacketHandler
    {
        #region Instantiation

        public GodModeHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task GodMode(GodModePacket godModePacket)
        {
            //Session.AddLogsCmd(godModePacket);
            Session.Character.HasGodMode = !Session.Character.HasGodMode;
            MessageExtension.SendGrey(Session, "[Server]: Command executed successfully");
        }

        #endregion
    }
}