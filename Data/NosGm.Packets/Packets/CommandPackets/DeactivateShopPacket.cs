using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateShop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateShopPacket : PacketDefinition
    {

    }
}
