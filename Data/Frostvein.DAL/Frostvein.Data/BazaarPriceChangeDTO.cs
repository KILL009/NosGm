using System;

namespace Frostvein.Data
{
    public enum BazaarPriceChangeResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        StateChanged = 2,
        InvalidPrice = 3,
        MissingSchema = 4,
        Error = 5
    }

    /// <summary>
    /// Authoritative price-change request for an existing bazaar publication.
    /// </summary>
    public sealed class BazaarPriceChangeDTO
    {
        public Guid OperationId { get; set; }

        public long BazaarItemId { get; set; }

        public long SellerAccountId { get; set; }

        public long SellerCharacterId { get; set; }

        public Guid BazaarItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        public long ExpectedPrice { get; set; }

        public long NewPrice { get; set; }

        public long MaximumGold { get; set; }
    }
}
