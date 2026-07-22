using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$Configuration", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class ConfigurationPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Type { get; set; }

        #endregion
    }
}
