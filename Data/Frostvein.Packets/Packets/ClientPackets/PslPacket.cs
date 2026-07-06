using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
    [PacketHeader("psl")]
    public class PslPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public int Type { get; set; }

        #endregion
    }
}