using System;

namespace Frostvein.DAL.EF
{
    public class FamilySkillMission
    {
        #region Properties

        public long FamilySkillMissionId { get; set; }

        public long FamilyId { get; set; }

        public short ItemVNum { get; set; }

        public short CurrentValue { get; set; }

        public int TotalValue { get; set; }

        public DateTime Date { get; set; }

        #endregion
    }
}
