using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IFamilyCharacterDAO
    {
        #region Methods

        DeleteResult Delete(long characterId);

        SaveResult InsertOrUpdate(ref FamilyCharacterDTO character);

        FamilyCharacterDTO LoadByCharacterId(long characterId);

        IList<FamilyCharacterDTO> LoadByFamilyId(long familyId);

        FamilyCharacterDTO LoadById(long familyCharacterId);

        #endregion
    }
}