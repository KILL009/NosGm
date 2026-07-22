using NosGm.Domain;

namespace NosGm.Core.Interfaces.Packets.ClientPackets
{
    public interface ICharacterCreatePacket
    {
        #region Properties

        GenderType Gender { get; set; }

        HairColorType HairColor { get; set; }

        HairStyleType HairStyle { get; set; }

        string Name { get; set; }

        string OriginalContent { get; set; }

        byte Slot { get; set; }

        #endregion
    }
}