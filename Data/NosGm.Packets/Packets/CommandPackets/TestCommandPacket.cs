using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$Test", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class TestCommandPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Type { get; set; }

        [PacketIndex(1)]
        public int Value { get; set; }

        [PacketIndex(2, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}