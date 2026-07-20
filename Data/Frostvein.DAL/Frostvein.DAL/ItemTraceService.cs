using Frostvein.Data;
using Frostvein.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL
{
    /// <summary>
    /// Central entry point for recording item mutations. Callers should create one
    /// OperationId per business operation and increment Sequence for every affected item.
    /// </summary>
    public sealed class ItemTraceService
    {
        private static readonly Lazy<ItemTraceService> LazyInstance =
            new Lazy<ItemTraceService>(() => new ItemTraceService());

        private ItemTraceService()
        {
        }

        public static ItemTraceService Instance => LazyInstance.Value;

        public Guid BeginOperation() => Guid.NewGuid();

        public ItemTraceDTO Record(
            Guid operationId,
            int sequence,
            ItemTraceAction action,
            ItemTraceSource source,
            ItemInstanceDTO before,
            ItemInstanceDTO after,
            long? actorAccountId = null,
            long? actorCharacterId = null,
            string actorName = null,
            string reason = null,
            object metadata = null,
            bool isSuspicious = false)
        {
            if (operationId == Guid.Empty)
            {
                throw new ArgumentException("OperationId must be created once and reused for the whole operation.", nameof(operationId));
            }

            if (sequence < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            var item = after ?? before;
            if (item == null)
            {
                throw new ArgumentException("At least one item snapshot is required.");
            }

            if (before != null && after != null && before.Id != after.Id)
            {
                throw new ArgumentException("Before and after snapshots must describe the same item instance.");
            }

            var serial = item.EquipmentSerialId == Guid.Empty
                ? (Guid?)null
                : item.EquipmentSerialId;

            var trace = new ItemTraceDTO
            {
                OperationId = operationId,
                Sequence = sequence,
                OccurredAtUtc = DateTime.UtcNow,
                Action = action,
                Source = source,
                ItemInstanceId = item.Id,
                EquipmentSerialId = serial,
                ItemVNum = item.ItemVNum,
                AmountBefore = before?.Amount,
                AmountAfter = after?.Amount,
                OwnerCharacterIdBefore = before?.CharacterId,
                OwnerCharacterIdAfter = after?.CharacterId,
                InventoryTypeBefore = before?.Type,
                InventoryTypeAfter = after?.Type,
                SlotBefore = before?.Slot,
                SlotAfter = after?.Slot,
                ActorAccountId = actorAccountId,
                ActorCharacterId = actorCharacterId,
                ActorName = actorName,
                Reason = reason,
                Metadata = SerializeMetadata(metadata),
                IsSuspicious = isSuspicious
            };

            return DAOFactory.ItemTraceDAO.InsertIfMissing(trace);
        }

        public IEnumerable<ItemTraceDTO> GetHistory(Guid itemInstanceId, int take = 100) =>
            DAOFactory.ItemTraceDAO.LoadByItemInstanceId(itemInstanceId, take);

        public IEnumerable<ItemTraceDTO> GetSerialHistory(Guid equipmentSerialId, int take = 100) =>
            DAOFactory.ItemTraceDAO.LoadByEquipmentSerialId(equipmentSerialId, take);

        public IEnumerable<ItemTraceDTO> GetOperation(Guid operationId) =>
            DAOFactory.ItemTraceDAO.LoadByOperationId(operationId);

        private static string SerializeMetadata(object metadata)
        {
            if (metadata == null) return null;
            if (metadata is string text) return text;
            return JsonConvert.SerializeObject(metadata, Formatting.None);
        }
    }
}
