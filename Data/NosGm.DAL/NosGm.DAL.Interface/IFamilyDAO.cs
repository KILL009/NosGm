using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IFamilyDAO
    {
        #region Methods

        DeleteResult Delete(long familyId);

        SaveResult InsertOrUpdate(ref FamilyDTO family);

        IEnumerable<FamilyDTO> LoadAll();

        FamilyDTO LoadByCharacterId(long characterId);

        FamilyDTO LoadById(long familyId);

        FamilyDTO LoadByName(string name);

        #endregion
    }
}