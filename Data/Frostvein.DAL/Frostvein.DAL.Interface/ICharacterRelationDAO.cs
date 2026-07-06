using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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