using Frostvein.Data;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IItemTraceDAO
    {
        ItemTraceDTO InsertIfMissing(ItemTraceDTO trace);

        IEnumerable<ItemTraceDTO> LoadByItemInstanceId(Guid itemInstanceId, int take = 100);

        IEnumerable<ItemTraceDTO> LoadByEquipmentSerialId(Guid equipmentSerialId, int take = 100);

        IEnumerable<ItemTraceDTO> LoadByOperationId(Guid operationId);

        IEnumerable<ItemTraceDTO> LoadSuspicious(int take = 100);

        IEnumerable<DuplicateEquipmentSerialItemDTO> LoadCurrentItemsByEquipmentSerialId(
            Guid equipmentSerialId,
            int take = 100);

        IEnumerable<DuplicateEquipmentSerialItemDTO> LoadDuplicateEquipmentSerialItems(int takeGroups = 20);
    }
}
