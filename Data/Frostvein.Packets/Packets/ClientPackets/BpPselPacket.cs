using Frostvein.Core;

namespace Frostvein.GameObject.Packets.ClientPackets
{
    [PacketHeader("bp_psel")]
    public class BpPselPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Position { get; set; }

        [PacketIndex(1)]
        public byte? Level { get; set; }
    }
}
