using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateChat", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateChatPacket : PacketDefinition
    {

    }
}
