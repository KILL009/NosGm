using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateShop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateShopPacket : PacketDefinition
    {

    }
}
