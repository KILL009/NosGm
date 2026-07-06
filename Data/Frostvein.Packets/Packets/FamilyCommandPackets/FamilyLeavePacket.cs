using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.FamilyCommandPackets
{
    [PacketHeader("%FamilyLeave", PassNonParseablePacket = true, Authorities = new[] { AuthorityType.User })]
    public class FamilyLeavePacket : PacketDefinition
    {
    }
}