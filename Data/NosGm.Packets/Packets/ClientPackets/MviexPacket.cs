using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("mviex")]
    public class MviexPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte InventoryType { get; set; }

        [PacketIndex(1)]
        public short Slot { get; set; }

        [PacketIndex(2)]
        public short Amount { get; set; }

        [PacketIndex(3)]
        public short DestinationSlot { get; set; }
    }
}