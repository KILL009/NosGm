using Frostvein.Core;
namespace Frostvein.Packets.Packets.ServerPackets
{
    [PacketHeader("inbox")]
    public class InboxPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public string Data { get; set; }

        [PacketIndex(1)] public int Amount { get; set; }

        [PacketIndex(2)] public int Unknow2 { get; set; }

        [PacketIndex(3)] public string Title { get; set; }

        #endregion
    }
}
