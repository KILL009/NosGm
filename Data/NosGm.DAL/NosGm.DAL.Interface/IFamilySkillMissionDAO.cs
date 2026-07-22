using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IFamilySkillMissionDAO
    {
        #region Methods

        DeleteResult Delete(long itemVNum, long familyId);

        SaveResult InsertOrUpdate(ref FamilySkillMissionDTO familyskillmission);

        IList<FamilySkillMissionDTO> LoadByFamilyId(long familyId);

        void DailyReset(FamilySkillMissionDTO fsm);

        IEnumerable<FamilySkillMissionDTO> LoadAll();
        #endregion
    }
}
