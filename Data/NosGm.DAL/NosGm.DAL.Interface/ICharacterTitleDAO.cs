using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
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