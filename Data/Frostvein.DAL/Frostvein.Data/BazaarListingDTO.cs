using System;

namespace Frostvein.Data
{
    public enum BazaarListingResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        StateChanged = 2,
        NotEnoughGold = 3,
        ListingLimitReached = 4,
        InvalidItem = 5,
        InvalidPrice = 6,
        MissingSchema = 7,
        Error = 8
    }

    /// <summary>
    /// Complete before/after plan for moving one persistent item into the NosBazaar.
    /// The DAO revalidates every value under a serializable transaction.
    /// </summary>
    public sealed class BazaarListingDTO
    {
        public Guid OperationId { get; set; }

        public long SellerAccountId { get; set; }

        public long SellerCharacterId { get; set; }

        public long GoldBefore { get; set; }

        public long GoldAfter { get; set; }

        public long Tax { get; set; }

        public long MaximumGold { get; set; }

        public ItemInstanceDTO SourceBefore { get; set; }

        public ItemInstanceDTO SourceAfter { get; set; }

        public ItemInstanceDTO BazaarItemAfter { get; set; }

        public BazaarItemDTO Listing { get; set; }
    }
}
