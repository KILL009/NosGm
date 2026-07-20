using Frostvein.Data;
using Frostvein.Domain;
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

    public interface IGmCommandAuditDAO
    {
        bool IsAvailable();

        GmCommandAuditDTO Insert(GmCommandAuditDTO audit);

        IEnumerable<GmCommandAuditDTO> LoadRecent(int take = 30);

        IEnumerable<GmCommandAuditDTO> LoadByAccountId(long accountId, int take = 30);

        IEnumerable<GmCommandAuditDTO> LoadByCharacterId(long characterId, int take = 30);

        IEnumerable<GmCommandAuditDTO> LoadByCommand(string commandHeader, int take = 30);

        IEnumerable<GmCommandAuditDTO> LoadByOutcome(GmCommandAuditOutcome outcome, int take = 30);
    }

    public interface IStaffPermissionDAO
    {
        bool IsAvailable();

        StaffPermissionProfileDTO LoadByAccountId(long accountId);

        StaffPermissionProfileDTO Save(
            long accountId,
            long permissionMask,
            bool isEnabled,
            long? updatedByAccountId,
            long? updatedByCharacterId,
            string reason);
    }
}
