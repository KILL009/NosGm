using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
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