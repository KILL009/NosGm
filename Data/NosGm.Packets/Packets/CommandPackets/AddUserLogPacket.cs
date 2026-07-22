using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.CommandPackets
{
    [PacketHeader("$AddUserLog", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class AddUserLogPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Username { get; set; }

        #endregion

        #region Methods

        public static string ReturnHelp() => "$AddUserLog <Username>";

        #endregion
    }
}
