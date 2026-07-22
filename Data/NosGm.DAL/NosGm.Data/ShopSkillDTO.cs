using System;

namespace NosGm.Data
{
    [Serializable]
    public class ShopSkillDTO
    {
        #region Properties

        public int ShopId { get; set; }

        public int ShopSkillId { get; set; }

        public short SkillVNum { get; set; }

        public byte Slot { get; set; }

        public byte Type { get; set; }

        #endregion
    }
}