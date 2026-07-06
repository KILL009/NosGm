using Frostvein.Core;

namespace Frostvein.Packets.Packets.CommandPackets
{
    [PacketHeader("%Familydismiss", "%FamilyKick", "%Rejetdefamille")]
    public class FamilyKickPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public string Name { get; set; }

        #endregion
    }
}