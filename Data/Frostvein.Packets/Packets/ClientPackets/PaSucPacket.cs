using Frostvein.Core;

namespace Frostvein.GameObject.Packets.ClientPackets
{
    [PacketHeader("pa_suc")]
    public class PaSucPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int FirstData{ get; set; }

        [PacketIndex(1)]
        public int SecondData { get; set; }

        [PacketIndex(2)]
        public int ThirdData { get; set; }

        public string Value { get; set; }

        public short Slot { get; set; }
    }
}
