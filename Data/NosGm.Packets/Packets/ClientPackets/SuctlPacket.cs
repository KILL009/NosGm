using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("suctl")]
    public class SuctlPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public int CastId { get; set; }

        [PacketIndex(1)] public int Unknown2 { get; set; }

        [PacketIndex(2)] public int MateTransportId { get; set; }

        [PacketIndex(3)] public UserType TargetType { get; set; }

        [PacketIndex(4)] public long TargetId { get; set; }

        #endregion
    }
}