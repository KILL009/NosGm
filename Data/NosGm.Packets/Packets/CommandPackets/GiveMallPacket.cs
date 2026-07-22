using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$GiveMall", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class GiveMallPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short Amount { get; set; }

        [PacketIndex(1)]
        public string CharacterName { get; set; }

        public static string ReturnHelp() => "$GiveMall <Amount> <Nickname>";

        #endregion
    }
}