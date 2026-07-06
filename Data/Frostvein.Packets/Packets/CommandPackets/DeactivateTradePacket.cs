using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateTrade", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateTradePacket : PacketDefinition
    {

    }
}
