using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("ew_hsh")]
    public  class EwHshPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string PacketData { get; set; }

        #endregion
    }
}
