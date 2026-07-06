using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("fws")]
    public class FwsPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short ItemVNum { get; set; }
    }
}
