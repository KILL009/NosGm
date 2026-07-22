using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateDrop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateDropPacket : PacketDefinition
    {

    }
}
