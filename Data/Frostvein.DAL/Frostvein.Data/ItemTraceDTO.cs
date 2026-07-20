using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    /// <summary>
    /// Immutable audit event for one item mutation. ItemTrace rows must only be inserted,
    /// never updated or deleted during normal server operation.
    /// </summary>
    [Serializable]
    public sealed class ItemTraceDTO : SynchronizableBaseDTO
    {
        public Guid OperationId { get; set; }

        public int Sequence { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public ItemTraceAction Action { get; set; }

        public ItemTraceSource Source { get; set; }

        public Guid ItemInstanceId { get; set; }

        public Guid? EquipmentSerialId { get; set; }

        public short ItemVNum { get; set; }

        public int? AmountBefore { get; set; }

        public int? AmountAfter { get; set; }

        public long? OwnerCharacterIdBefore { get; set; }

        public long? OwnerCharacterIdAfter { get; set; }

        public InventoryType? InventoryTypeBefore { get; set; }

        public InventoryType? InventoryTypeAfter { get; set; }

        public short? SlotBefore { get; set; }

        public short? SlotAfter { get; set; }

        public long? ActorAccountId { get; set; }

        public long? ActorCharacterId { get; set; }

        public string ActorName { get; set; }

        public string Reason { get; set; }

        public string Metadata { get; set; }

        public bool IsSuspicious { get; set; }
    }
}
