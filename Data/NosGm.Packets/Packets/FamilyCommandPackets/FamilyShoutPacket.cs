using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.FamilyCommandPackets
{
    [PacketHeader("%FamilyShout", PassNonParseablePacket = true, Authorities = new[] { AuthorityType.User })]
    public class FamilyShoutPacket : PacketDefinition
    {
        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}