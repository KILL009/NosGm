using NosGm.Data;
using NosGm.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface ICharacterSkillDAO
    {
        #region Methods

        DeleteResult Delete(long characterId, short skillVNum);

        DeleteResult Delete(Guid id);

        CharacterSkillDTO InsertOrUpdate(CharacterSkillDTO dto);

        IEnumerable<CharacterSkillDTO> InsertOrUpdate(IEnumerable<CharacterSkillDTO> dtos);

        Task<CharacterSkillDTO> InsertOrUpdateAsync(CharacterSkillDTO dto);

        IEnumerable<CharacterSkillDTO> LoadByCharacterId(long characterId);

        CharacterSkillDTO LoadById(Guid id);

        IEnumerable<Guid> LoadKeysByCharacterId(long characterId);

        #endregion
    }
}