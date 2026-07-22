using NosGm.Core;

namespace NosGm.Packets.Packets.FamilyCommandPackets
{
    [PacketHeader("%Familyinvite", "%Invitationdefamille")]
    public class FamilyInvitePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)] public string Name { get; set; }

        #endregion
    }
}