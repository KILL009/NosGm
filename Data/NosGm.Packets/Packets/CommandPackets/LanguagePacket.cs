using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$Language", PassNonParseablePacket = true, Authority = AuthorityType.User)]
    public class LanguagePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string Culture { get; set; }

        public static string ReturnHelp() => "$Language <en|es|de|fr|it|pl|cs|ru|ja|zh>";
    }
}
