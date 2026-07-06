using Frostvein.Packets.Packets.ClientPackets;
using System;

namespace Frostvein.Packets.CustomPackets
{
    public class RcsPacketModel
    {
        public CSListPacket Packet { get; set; }

        public long CharacterId { get; set; }
    }
}
