using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IItemInstanceDAO
    {
        #region Methods

        DeleteResult Delete(Guid id);

        DeleteResult DeleteFromSlotAndType(long characterId, short slot, InventoryType type);

        DeleteResult DeleteGuidList(IEnumerable<Guid> guids);

        ItemInstanceDTO InsertOrUpdate(ItemInstanceDTO dto, long characterId = 0);

        IEnumerable<ItemInstanceDTO> InsertOrUpdate(IEnumerable<ItemInstanceDTO> dtos);

        SaveResult InsertOrUpdateFromList(IEnumerable<ItemInstanceDTO> items);

        Task<SaveResult> InsertOrUpdateFromListAsync(IEnumerable<ItemInstanceDTO> items);

        IEnumerable<ItemInstanceDTO> LoadByCharacterId(long characterId);

        ItemInstanceDTO LoadById(Guid id);

        ItemInstanceDTO LoadBySlotAndType(long characterId, short slot, InventoryType type);

        IEnumerable<ItemInstanceDTO> LoadByType(long characterId, InventoryType type);

        IList<Guid> LoadSlotAndTypeByCharacterId(long characterId);

        #endregion
    }
}
