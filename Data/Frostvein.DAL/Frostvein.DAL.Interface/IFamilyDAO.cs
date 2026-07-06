using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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