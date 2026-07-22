using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateBazaar", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class ActivateBazaarPacket : PacketDefinition
    {
        
    }
}
