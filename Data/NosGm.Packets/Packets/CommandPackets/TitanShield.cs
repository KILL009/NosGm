using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$TitanShield", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class TitanShieldPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string Action { get; set; }

        [PacketIndex(1)]
        public int Type { get; set; }

        [PacketIndex(2)]
        public int Value { get; set; }

        [PacketIndex(3, SerializeToEnd = true)]
        public string Message { get; set; }

      
    }
}