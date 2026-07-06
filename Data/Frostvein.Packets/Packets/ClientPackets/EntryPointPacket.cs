using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("Frostvein.EntryPoint", IsCharScreen = true, Amount = 3)]
    public class FrostveinEntryPointPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string PacketData { get; set; }

        #endregion

        //TODO: Find defined values
    }
}