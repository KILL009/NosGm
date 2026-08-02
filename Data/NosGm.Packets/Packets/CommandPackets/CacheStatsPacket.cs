using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$CacheStats", PassNonParseablePacket = true, Authority = AuthorityType.GM)]
    public class CacheStatsPacket : PacketDefinition
    {
        public static string ReturnHelp() => "$CacheStats";
    }
}
