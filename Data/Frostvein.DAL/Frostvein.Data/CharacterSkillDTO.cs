using System;

namespace Frostvein.Data
{
    [Serializable]
    public class CharacterSkillDTO : SynchronizableBaseDTO
    {
        #region Properties

        public long CharacterId { get; set; }

        public short SkillVNum { get; set; }

        public bool IsTattoo { get; set; }

        public byte TattooLevel { get; set; }

        public bool IsPartnerSkill { get; set; }

        #endregion
    }
}