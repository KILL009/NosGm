using Frostvein.Data;
using Frostvein.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface IQuicklistEntryDAO
    {
        #region Methods

        DeleteResult Delete(Guid id);

        QuicklistEntryDTO InsertOrUpdate(QuicklistEntryDTO dto);

        IEnumerable<QuicklistEntryDTO> InsertOrUpdate(IEnumerable<QuicklistEntryDTO> dtos);

        Task<QuicklistEntryDTO> InsertOrUpdateAsync(QuicklistEntryDTO dto);

        IEnumerable<QuicklistEntryDTO> LoadByCharacterId(long characterId);

        QuicklistEntryDTO LoadById(Guid id);

        IEnumerable<Guid> LoadKeysByCharacterId(long characterId);

        #endregion
    }
}