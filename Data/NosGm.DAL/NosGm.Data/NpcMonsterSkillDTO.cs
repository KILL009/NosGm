using System;

namespace NosGm.Data
{
    [Serializable]
    public class NpcMonsterSkillDTO
    {
        #region Properties

        public long NpcMonsterSkillId { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short Rate { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}