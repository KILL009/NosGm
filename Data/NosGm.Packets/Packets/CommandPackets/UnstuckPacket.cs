using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$Unstuck", PassNonParseablePacket = true, Authority = AuthorityType.User)]
    public class UnstuckPacket : PacketDefinition
    {
        #region Methods

        public static string ReturnHelp() => "$Unstuck";

        #endregion
    }
}