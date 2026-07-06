using System;

namespace Frostvein.Data
{
    [Serializable]
    public class BCardDTO
    {
        #region Properties

        public int BCardId { get; set; }

        public short? CardId { get; set; }

        public byte CastType { get; set; }

        public int FirstData { get; set; }

        public bool IsLevelDivided { get; set; }

        public bool IsLevelScaled { get; set; }

        public short? ItemVNum { get; set; }

        public short? NpcMonsterVNum { get; set; }

        public int SecondData { get; set; }

        public short? SkillVNum { get; set; }

        public byte SubType { get; set; }

        public int ThirdData { get; set; }

        public byte Type { get; set; }

        #endregion
    }
}