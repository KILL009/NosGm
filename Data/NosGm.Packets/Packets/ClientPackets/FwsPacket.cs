using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("fws")]
    public class FwsPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short ItemVNum { get; set; }
    }
}
