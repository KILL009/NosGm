using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface ICharacterRelationDAO
    {
        #region Methods

        DeleteResult Delete(long characterRelationId);

        SaveResult InsertOrUpdate(ref CharacterRelationDTO characterRelation);

        IEnumerable<CharacterRelationDTO> LoadAll();

        CharacterRelationDTO LoadById(long characterId);

        #endregion
    }
}