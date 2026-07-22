using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateDrop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateDropPacket : PacketDefinition
    {

    }
}
