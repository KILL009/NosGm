using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateTrade", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateTradePacket : PacketDefinition
    {

    }
}
