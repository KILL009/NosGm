using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateShop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateShopPacket : PacketDefinition
    {

    }
}
