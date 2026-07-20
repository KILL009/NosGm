using Frostvein.Domain;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Append-only record emitted centrally for every staff command that reaches a
    /// handler. It deliberately stores bounded, sanitized command text.
    /// </summary>
    [Serializable]
    public sealed class GmCommandAuditDTO
    {
        public long AuditId { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public long? AccountId { get; set; }

        public long? CharacterId { get; set; }

        public string CharacterName { get; set; }

        public AuthorityType Authority { get; set; }

        public string CommandHeader { get; set; }

        public string CommandText { get; set; }

        public AuthorityType RequiredAuthority { get; set; }

        public GmCommandAuditOutcome Outcome { get; set; }

        public string IpAddress { get; set; }

        public int ChannelId { get; set; }

        public short? MapId { get; set; }

        public int? SessionId { get; set; }

        public string Failure { get; set; }
    }

    public enum TradeCommitResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        Conflict = 2,
        MissingSchema = 3,
        Error = 4
    }

    /// <summary>
    /// Complete before/after state used to commit both sides of one trade in a single
    /// SQL transaction. Only items affected by the trade are included.
    /// </summary>
    [Serializable]
    public sealed class TradeCommitDTO
    {
        public TradeCommitDTO()
        {
            BeforeItems = new List<ItemInstanceDTO>();
            AfterItems = new List<ItemInstanceDTO>();
        }

        public Guid OperationId { get; set; }

        public long FirstCharacterId { get; set; }

        public long SecondCharacterId { get; set; }

        public long FirstGoldBefore { get; set; }

        public long FirstGoldAfter { get; set; }

        public long FirstGoldBankBefore { get; set; }

        public long FirstGoldBankAfter { get; set; }

        public long SecondGoldBefore { get; set; }

        public long SecondGoldAfter { get; set; }

        public long SecondGoldBankBefore { get; set; }

        public long SecondGoldBankAfter { get; set; }

        public List<ItemInstanceDTO> BeforeItems { get; set; }

        public List<ItemInstanceDTO> AfterItems { get; set; }
    }
}
