using System;
using System.Collections.Generic;

namespace Frostvein.Data
{
    public enum BazaarRecollectResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        StateChanged = 2,
        NoInventorySpace = 3,
        GoldLimit = 4,
        MissingSchema = 5,
        Error = 6
    }

    /// <summary>
    /// Complete before/after plan for collecting one bazaar listing. The source
    /// bazaar row, remaining item, seller inventory and proceeds are committed
    /// together so a simultaneous purchase cannot duplicate the remaining stack.
    /// </summary>
    [Serializable]
    public sealed class BazaarRecollectDTO
    {
        public BazaarRecollectDTO()
        {
            ItemsBefore = new List<ItemInstanceDTO>();
            ItemsAfter = new List<ItemInstanceDTO>();
        }

        public Guid OperationId { get; set; }

        public long BazaarItemId { get; set; }

        public long SellerCharacterId { get; set; }

        public Guid BazaarItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public short ListingAmount { get; set; }

        public short RemainingAmount { get; set; }

        public short SoldAmount { get; set; }

        public long UnitPrice { get; set; }

        public long Tax { get; set; }

        public long Proceeds { get; set; }

        public long GoldBefore { get; set; }

        public long GoldAfter { get; set; }

        public List<ItemInstanceDTO> ItemsBefore { get; set; }

        public List<ItemInstanceDTO> ItemsAfter { get; set; }
    }
}
