using Frostvein.Core;

namespace Frostvein.Packets.Packets.ServerPackets
{
    [PacketHeader("tit_eq")]
    public class TitEqPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public byte Type { get; set; }

        [PacketIndex(1)] public short ItemVnum { get; set; }

        #endregion
    }
}