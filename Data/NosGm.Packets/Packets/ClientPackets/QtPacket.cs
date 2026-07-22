using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("qt")]
    public class QtPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(1)] public int Data { get; set; }

        [PacketIndex(0)] public short Type { get; set; }

        #endregion
    }
}