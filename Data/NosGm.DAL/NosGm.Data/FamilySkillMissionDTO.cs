using System;

namespace NosGm.Data
{
    [Serializable]
    public class FamilySkillMissionDTO
    {
        public long FamilyId { get; set; }

        public short ItemVNum { get; set; }

        public long FamilySkillMissionId { get; set; }

        public short CurrentValue { get; set; }

        public int TotalValue { get; set; }

        public DateTime Date { get; set; }

    }
}
