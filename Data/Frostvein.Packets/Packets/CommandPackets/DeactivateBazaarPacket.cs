using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$DeactivateBazaar", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class DeactivateBazaarPacket : PacketDefinition
    {

    }
}
