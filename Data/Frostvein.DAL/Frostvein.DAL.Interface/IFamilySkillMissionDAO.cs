using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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
