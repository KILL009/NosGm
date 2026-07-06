using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
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