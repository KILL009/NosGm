using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateTrade", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateTradePacket : PacketDefinition
    {

    }
}
