using Frostvein.Extension.Extension.Command;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Translator;

namespace Frostvein.Handler.PacketHandler.Command
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