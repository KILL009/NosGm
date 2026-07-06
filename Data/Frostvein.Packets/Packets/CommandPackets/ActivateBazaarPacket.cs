using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$ActivateBazaar", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class ActivateBazaarPacket : PacketDefinition
    {
        
    }
}
