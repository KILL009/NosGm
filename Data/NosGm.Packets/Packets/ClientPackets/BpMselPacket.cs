using NosGm.Core;

namespace NosGm.GameObject.Packets.ClientPackets
{
    [PacketHeader("bp_msel")]
    public class BpMselPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int QuestId { get; set; }
    }
}
