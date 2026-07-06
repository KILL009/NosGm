using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("sreq")]
    public class SreqPacket : PacketDefinition
    {
        [PacketIndex(0)] public string Argument { get; set; }
    }
}
