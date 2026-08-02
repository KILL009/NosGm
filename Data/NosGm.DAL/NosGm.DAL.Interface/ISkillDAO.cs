using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface ISkillDAO
    {
        #region Methods

        SkillDTO Insert(SkillDTO skill);

        void Insert(List<SkillDTO> skills);

        SaveResult InsertOrUpdate(SkillDTO skill);

        IEnumerable<SkillDTO> LoadAll();

        CacheStatisticsSnapshot GetCacheStatistics();

        SkillDTO LoadById(short skillId);

        #endregion
    }
}