using NosGm.Extension.Extension.Command;
using NosGm.Packets.Packets.CommandPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension.Translator;

namespace NosGm.Handler.PacketHandler.Command
{
    public class TimeSpaceMessagePacketHandler : IPacketHandler
    {
        #region Instantiation

        public TimeSpaceMessagePacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void Generate(TimeSpaceMessagePacket timeSpaceMessagePacket)
        {
           
        }

        #endregion
    }
}