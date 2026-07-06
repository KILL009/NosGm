using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateChat", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateChatPacket : PacketDefinition
    {

    }
}
