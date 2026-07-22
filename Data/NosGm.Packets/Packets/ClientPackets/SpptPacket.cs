using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("sppt")]
    public class SpptPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Type { get; set; }

        [PacketIndex(1)]
        public short ItemToUpgrade { get; set; }

        [PacketIndex(2)]
        public short? ItemToFuse { get; set; }

        #endregion
    }
}