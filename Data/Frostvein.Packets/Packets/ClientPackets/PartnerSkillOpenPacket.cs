using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("ps_op", PassNonParseablePacket = true)]
    public class PartnerSkillOpenPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(1)] public byte CastId { get; set; }

        [PacketIndex(2)] public bool JustDoIt { get; set; }

        [PacketIndex(0)] public byte PetId { get; set; }

        #endregion
    }
}