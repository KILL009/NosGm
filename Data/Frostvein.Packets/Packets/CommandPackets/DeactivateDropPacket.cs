using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateDrop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateDropPacket : PacketDefinition
    {

    }
}
