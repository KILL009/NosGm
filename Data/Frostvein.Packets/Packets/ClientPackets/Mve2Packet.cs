using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("mve2")]
    public class Mve2Packet : PacketDefinition
    {
        [PacketIndex(0)]
        public byte SourceInventoryId { get; set; }

        [PacketIndex(1)]
        public short SourceSlot { get; set; }

        [PacketIndex(2)]
        public byte DestinationInventoryId { get; set; }

        [PacketIndex(3)]
        public short DestinationSlot { get; set; }
    }
}