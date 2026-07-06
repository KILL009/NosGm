using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface ICharacterQuestDAO
    {
        #region Methods

        DeleteResult Delete(long characterId, long questId);

        CharacterQuestDTO InsertOrUpdate(CharacterQuestDTO quest);

        IEnumerable<CharacterQuestDTO> LoadByCharacterId(long characterId);

        IEnumerable<Guid> LoadKeysByCharacterId(long characterId);

        #endregion
    }
}