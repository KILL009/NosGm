using System;

namespace Frostvein.Data
{
    [Serializable]
    public class ComboDTO
    {
        #region Properties

        public short Animation { get; set; }

        public int ComboId { get; set; }

        public short Effect { get; set; }

        public short Hit { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}