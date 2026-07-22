using NosGm.Packets.Packets.ClientPackets;
using System;

namespace NosGm.Packets.CustomPackets
{
    public class RcsPacketModel
    {
        public CSListPacket Packet { get; set; }

        public long CharacterId { get; set; }
    }
}
