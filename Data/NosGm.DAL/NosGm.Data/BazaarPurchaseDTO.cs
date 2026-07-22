using System;
using System.Collections.Generic;

namespace NosGm.Data
{
    public enum BazaarPurchaseResult
    {
        Success = 0,
        AlreadyCommitted = 1,
        StateChanged = 2,
        NotEnoughGold = 3,
        NoInventorySpace = 4,
        MissingSchema = 5,
        Error = 6
    }

    /// <summary>
    /// Complete immutable plan for one bazaar purchase. The DAO revalidates the
    /// listing, remaining amount, buyer balances and every affected inventory row
    /// before committing them together in one serializable SQL transaction.
    /// </summary>
    [Serializable]
    public sealed class BazaarPurchaseDTO
    {
        public BazaarPurchaseDTO()
        {
            BuyerItemsBefore = new List<ItemInstanceDTO>();
            BuyerItemsAfter = new List<ItemInstanceDTO>();
        }

        public Guid OperationId { get; set; }

        public long BazaarItemId { get; set; }

        public long BuyerAccountId { get; set; }

        public long BuyerCharacterId { get; set; }

        public long SellerCharacterId { get; set; }

        public Guid BazaarItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public short Amount { get; set; }

        public long UnitPrice { get; set; }

        public short BazaarAmountBefore { get; set; }

        public short BazaarAmountAfter { get; set; }

        public long BuyerGoldBefore { get; set; }

        public long BuyerGoldAfter { get; set; }

        public long BuyerGoldBankBefore { get; set; }

        public long BuyerGoldBankAfter { get; set; }

        public List<ItemInstanceDTO> BuyerItemsBefore { get; set; }

        public List<ItemInstanceDTO> BuyerItemsAfter { get; set; }
    }
}
