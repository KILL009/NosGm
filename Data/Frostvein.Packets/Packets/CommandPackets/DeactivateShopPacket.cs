using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateShop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateShopPacket : PacketDefinition
    {

    }
}
