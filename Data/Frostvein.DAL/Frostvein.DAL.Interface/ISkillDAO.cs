using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface ISkillDAO
    {
        #region Methods

        SkillDTO Insert(SkillDTO skill);

        void Insert(List<SkillDTO> skills);

        SaveResult InsertOrUpdate(SkillDTO skill);

        IEnumerable<SkillDTO> LoadAll();

        SkillDTO LoadById(short skillId);

        #endregion
    }
}