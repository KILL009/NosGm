using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.ServerPackets
{
    [PacketHeader("gbox")]
    public class GboxPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(1)] public long Amount { get; set; }

        [PacketIndex(2)] public byte Option { get; set; }

        [PacketIndex(0)] public BankActionType Type { get; set; }

        #endregion
    }
}