using NosGm.Core;
using NosGm.Domain;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("u_ps")]
    public class UsePartnerSkillPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public long TransportId { get; set; }

        [PacketIndex(1)] public UserType TargetType { get; set; }

        [PacketIndex(2)] public long TargetId { get; set; }

        [PacketIndex(3)] public byte CastId { get; set; }

        [PacketIndex(4)] public short? MapX { get; set; }

        [PacketIndex(5)] public short? MapY { get; set; }

        #endregion
    }
}