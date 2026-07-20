using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    public sealed class BazaarAuditStatusDTO
    {
        public int ListingOperationAvailable { get; set; }

        public int PurchaseOperationAvailable { get; set; }

        public int PriceChangeOperationAvailable { get; set; }

        public int RecollectOperationAvailable { get; set; }

        public long ActiveListingCount { get; set; }

        public long BazaarInventoryItemCount { get; set; }

        public long ListingOperationCount { get; set; }

        public long PurchaseOperationCount { get; set; }

        public long PriceChangeOperationCount { get; set; }

        public long RecollectOperationCount { get; set; }

        public bool IsComplete => ListingOperationAvailable == 1 &&
                                  PurchaseOperationAvailable == 1 &&
                                  PriceChangeOperationAvailable == 1 &&
                                  RecollectOperationAvailable == 1;
    }

    public sealed class BazaarAuditEventDTO
    {
        public Guid OperationId { get; set; }

        public BazaarAuditEventType EventType { get; set; }

        public long BazaarItemId { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public long? AccountId { get; set; }

        public long PrimaryCharacterId { get; set; }

        public long? CounterpartyCharacterId { get; set; }

        public Guid ItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public int Amount { get; set; }

        public int RemainingAmount { get; set; }

        public long UnitPrice { get; set; }

        public long PreviousUnitPrice { get; set; }

        public long GoldDelta { get; set; }
    }

    public sealed class BazaarAuditListingDTO
    {
        public long BazaarItemId { get; set; }

        public long SellerAccountId { get; set; }

        public long SellerCharacterId { get; set; }

        public string SellerName { get; set; }

        public Guid ItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public int ListedAmount { get; set; }

        public int RemainingAmount { get; set; }

        public long UnitPrice { get; set; }

        public DateTime DateStart { get; set; }

        public short Duration { get; set; }

        public bool IsPackage { get; set; }

        public bool MedalUsed { get; set; }

        public byte InventoryType { get; set; }

        public long ItemOwnerCharacterId { get; set; }

        public Guid? EquipmentSerialId { get; set; }

        public int PurchaseCount { get; set; }

        public int PurchasedAmount { get; set; }

        public int HasListingOperation { get; set; }
    }

    public sealed class BazaarAuditAnomalyDTO
    {
        public BazaarAuditSeverity Severity { get; set; }

        public string Code { get; set; }

        public long? BazaarItemId { get; set; }

        public Guid? ItemInstanceId { get; set; }

        public long? CharacterId { get; set; }

        public short? ItemVNum { get; set; }

        public DateTime? OccurredAtUtc { get; set; }

        public string Detail { get; set; }
    }
}
