using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("sreq")]
    public class SreqPacket : PacketDefinition
    {
        [PacketIndex(0)] public string Argument { get; set; }
    }
}
