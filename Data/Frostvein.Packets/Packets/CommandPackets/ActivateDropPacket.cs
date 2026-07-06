using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateDrop", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateDropPacket : PacketDefinition
    {

    }
}
