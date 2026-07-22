using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.FamilyCommandPackets
{
    [PacketHeader("%FamilyLeave", PassNonParseablePacket = true, Authorities = new[] { AuthorityType.User })]
    public class FamilyLeavePacket : PacketDefinition
    {
    }
}