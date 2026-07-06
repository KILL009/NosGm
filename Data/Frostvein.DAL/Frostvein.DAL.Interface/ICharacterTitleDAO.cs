using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface ICharacterTitleDAO
    {
        #region Methods

        DeleteResult Delete(long characterTitleId);

        SaveResult InsertOrUpdate(ref CharacterTitleDTO characterTitle);

        Task<CharacterTitleDTO> InsertOrUpdateAsync(CharacterTitleDTO characterTitle);

        IEnumerable<CharacterTitleDTO> LoadByCharacterId(long characterId);

        #endregion
    }
}