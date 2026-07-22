using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$PspXp", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class PartnerSpXpPacket : PacketDefinition
    {
        #region Properties

        public static string ReturnHelp() => "$PspXp";

        #endregion
    }
}
