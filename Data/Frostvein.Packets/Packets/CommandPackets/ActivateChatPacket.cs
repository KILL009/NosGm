using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateChat", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateChatPacket : PacketDefinition
    {

    }
}
