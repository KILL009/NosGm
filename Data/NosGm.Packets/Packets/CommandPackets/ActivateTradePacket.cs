using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateTrade", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateTradePacket : PacketDefinition
    {

    }
}
