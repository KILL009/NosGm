using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IFamilyLogDAO
    {
        #region Methods

        DeleteResult Delete(long familyLogId);

        SaveResult InsertOrUpdate(ref FamilyLogDTO familyLog);

        IEnumerable<FamilyLogDTO> LoadByFamilyId(long familyId);

        #endregion
    }
}