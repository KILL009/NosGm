using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateChat", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class ActivateChatPacket : PacketDefinition
    {

    }
}
