using Frostvein.Core;
using Frostvein.Domain;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("$RemoveUserLog", PassNonParseablePacket = true, Authority = AuthorityType.ADMIN)]
    public class RemoveUserLogPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Username { get; set; }

        #endregion

        #region Methods

        public static string ReturnHelp() => "$RemoveUserLog <Username>";

        #endregion
    }
}