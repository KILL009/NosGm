using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateBazaar", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateBazaarPacket : PacketDefinition
    {

    }
}
