using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using NosGm.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace NosGm.DAL.DAO
{
    /// <summary>
    /// Read-only projections over the atomic bazaar ledgers and current bazaar state.
    /// This component never changes listings, balances or item instances.
    /// </summary>
    public sealed class BazaarAuditDAO
    {
        private const int MaximumTake = 100;
        private static int _failureLogged;

        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    const string sql = @"
SELECT CASE WHEN OBJECT_ID(N'dbo.BazaarItem', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.ItemInstance', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.Character', N'U') IS NOT NULL
            THEN 1 ELSE 0 END;";
                    return context.Database.SqlQuery<int>(sql).Single() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public BazaarAuditStatusDTO LoadStatus()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    BazaarAuditStatusDTO status = ReadAvailability(context);
                    status.ActiveListingCount = CountTable(context, "dbo.BazaarItem");
                    status.BazaarInventoryItemCount = context.Database.SqlQuery<long>(
                        "SELECT COUNT_BIG(*) FROM dbo.ItemInstance WHERE [Type] = 9;").Single();
                    status.ListingOperationCount = CountOptional(
                        context, "dbo.BazaarListingOperation", status.ListingOperationAvailable);
                    status.PurchaseOperationCount = CountOptional(
                        context, "dbo.BazaarPurchaseOperation", status.PurchaseOperationAvailable);
                    status.PriceChangeOperationCount = CountOptional(
                        context, "dbo.BazaarPriceChangeOperation", status.PriceChangeOperationAvailable);
                    status.RecollectOperationCount = CountOptional(
                        context, "dbo.BazaarRecollectOperation", status.RecollectOperationAvailable);
                    return status;
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to read bazaar audit status.", exception);
                return new BazaarAuditStatusDTO();
            }
        }

        public BazaarAuditListingDTO LoadListing(long bazaarItemId)
        {
            if (bazaarItemId <= 0) return null;

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    BazaarAuditStatusDTO status = ReadAvailability(context);
                    string purchaseCount = status.PurchaseOperationAvailable == 1
                        ? "(SELECT COUNT(*) FROM dbo.BazaarPurchaseOperation p WHERE p.BazaarItemId = b.BazaarItemId)"
                        : "CAST(0 AS int)";
                    string purchasedAmount = status.PurchaseOperationAvailable == 1
                        ? "(SELECT COALESCE(SUM(CAST(p.Amount AS int)), 0) FROM dbo.BazaarPurchaseOperation p WHERE p.BazaarItemId = b.BazaarItemId)"
                        : "CAST(0 AS int)";
                    string hasListingOperation = status.ListingOperationAvailable == 1
                        ? "CASE WHEN EXISTS (SELECT 1 FROM dbo.BazaarListingOperation l WHERE l.BazaarItemId = b.BazaarItemId) THEN 1 ELSE 0 END"
                        : "CAST(0 AS int)";

                    string sql = @"
SELECT b.BazaarItemId,
       b.AccountId AS SellerAccountId,
       b.SellerId AS SellerCharacterId,
       c.Name AS SellerName,
       b.ItemInstanceId,
       COALESCE(i.ItemVNum, CAST(0 AS smallint)) AS ItemVNum,
       CAST(b.Amount AS int) AS ListedAmount,
       COALESCE(i.Amount, -1) AS RemainingAmount,
       b.Price AS UnitPrice,
       b.DateStart,
       b.Duration,
       b.IsPackage,
       b.MedalUsed,
       COALESCE(CAST(i.[Type] AS tinyint), CAST(255 AS tinyint)) AS InventoryType,
       COALESCE(i.CharacterId, CAST(0 AS bigint)) AS ItemOwnerCharacterId,
       i.EquipmentSerialId,
       " + purchaseCount + @" AS PurchaseCount,
       " + purchasedAmount + @" AS PurchasedAmount,
       " + hasListingOperation + @" AS HasListingOperation
FROM dbo.BazaarItem b
LEFT JOIN dbo.Character c ON c.CharacterId = b.SellerId
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE b.BazaarItemId = @BazaarItemId;";

                    return context.Database.SqlQuery<BazaarAuditListingDTO>(sql,
                        new SqlParameter("@BazaarItemId", bazaarItemId)).FirstOrDefault();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to read a bazaar listing snapshot.", exception);
                return null;
            }
        }

        public IEnumerable<BazaarAuditEventDTO> LoadRecent(int take = 20)
        {
            return LoadEvents(null, null, take);
        }

        public IEnumerable<BazaarAuditEventDTO> LoadByListing(long bazaarItemId, int take = 30)
        {
            if (bazaarItemId <= 0) return Enumerable.Empty<BazaarAuditEventDTO>();
            return LoadEvents("e.BazaarItemId = @BazaarItemId",
                new object[] { new SqlParameter("@BazaarItemId", bazaarItemId) }, take);
        }

        public IEnumerable<BazaarAuditEventDTO> LoadByCharacter(long characterId, int take = 30)
        {
            if (characterId <= 0) return Enumerable.Empty<BazaarAuditEventDTO>();
            return LoadEvents(
                "(e.PrimaryCharacterId = @CharacterId OR e.CounterpartyCharacterId = @CharacterId)",
                new object[] { new SqlParameter("@CharacterId", characterId) }, take);
        }

        public IEnumerable<BazaarAuditEventDTO> LoadByItem(Guid itemInstanceId, int take = 30)
        {
            if (itemInstanceId == Guid.Empty) return Enumerable.Empty<BazaarAuditEventDTO>();
            return LoadEvents("e.ItemInstanceId = @ItemInstanceId",
                new object[] { new SqlParameter("@ItemInstanceId", itemInstanceId) }, take);
        }

        public IEnumerable<BazaarAuditAnomalyDTO> LoadAnomalies(int take = 30)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    BazaarAuditStatusDTO status = ReadAvailability(context);
                    var queries = BuildCurrentStateAnomalyQueries();

                    if (status.ListingOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(3 AS tinyint) AS Severity,
       CAST(N'LISTING_LEDGER_ARITHMETIC' AS nvarchar(64)) AS Code,
       CAST(l.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(l.BazaarItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(l.SellerCharacterId AS bigint) AS CharacterId,
       CAST(l.ItemVNum AS smallint) AS ItemVNum,
       CAST(l.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Listing operation quantities, tax or gold delta do not balance.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarListingOperation l
WHERE l.ListedAmount <= 0 OR l.UnitPrice <= 0 OR l.Tax < 0
   OR l.AmountAfter <> l.AmountBefore - l.ListedAmount
   OR l.GoldAfter <> l.GoldBefore - l.Tax");
                    }

                    if (status.PurchaseOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(3 AS tinyint) AS Severity,
       CAST(N'PURCHASE_LEDGER_ARITHMETIC' AS nvarchar(64)) AS Code,
       CAST(p.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(p.BazaarItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(p.BuyerCharacterId AS bigint) AS CharacterId,
       CAST(p.ItemVNum AS smallint) AS ItemVNum,
       CAST(p.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Purchase quantities or participants do not balance.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarPurchaseOperation p
LEFT JOIN dbo.Character seller ON seller.CharacterId = p.SellerCharacterId
WHERE p.Amount <= 0 OR p.UnitPrice <= 0 OR p.BazaarAmountAfter < 0
   OR p.BazaarAmountAfter <> p.BazaarAmountBefore - p.Amount
   OR p.BuyerCharacterId = p.SellerCharacterId
   OR seller.AccountId = p.BuyerAccountId");
                    }

                    if (status.PriceChangeOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(2 AS tinyint) AS Severity,
       CAST(N'PRICE_LEDGER_INVALID' AS nvarchar(64)) AS Code,
       CAST(p.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(p.BazaarItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(p.SellerCharacterId AS bigint) AS CharacterId,
       CAST(p.ItemVNum AS smallint) AS ItemVNum,
       CAST(p.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Price change contains a non-positive or unchanged price.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarPriceChangeOperation p
WHERE p.OldUnitPrice <= 0 OR p.NewUnitPrice <= 0 OR p.OldUnitPrice = p.NewUnitPrice");
                    }

                    if (status.RecollectOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(3 AS tinyint) AS Severity,
       CAST(N'RECOLLECT_LEDGER_ARITHMETIC' AS nvarchar(64)) AS Code,
       CAST(r.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(r.BazaarItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(r.SellerCharacterId AS bigint) AS CharacterId,
       CAST(r.ItemVNum AS smallint) AS ItemVNum,
       CAST(r.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Recollection quantities, proceeds or gold delta do not balance.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarRecollectOperation r
WHERE r.RemainingAmount < 0 OR r.SoldAmount < 0 OR r.UnitPrice <= 0 OR r.Tax < 0
   OR r.SoldAmount <> r.ListingAmount - r.RemainingAmount
   OR r.Proceeds <> CAST(r.SoldAmount AS bigint) * r.UnitPrice - r.Tax
   OR r.GoldAfter <> r.GoldBefore + r.Proceeds");

                        queries.Add(@"
SELECT CAST(3 AS tinyint) AS Severity,
       CAST(N'RECOLLECTED_LISTING_STILL_ACTIVE' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId,
       CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(r.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'A recollection was committed but the listing still exists.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.BazaarRecollectOperation r ON r.BazaarItemId = b.BazaarItemId
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId");
                    }

                    if (status.ListingOperationAvailable == 1 &&
                        status.PurchaseOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(3 AS tinyint) AS Severity,
       CAST(N'ACTIVE_PURCHASE_TOTAL_MISMATCH' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId,
       CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(l.CompletedAtUtc AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Purchase totals do not match the active listing remaining amount.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
JOIN dbo.BazaarListingOperation l ON l.BazaarItemId = b.BazaarItemId
OUTER APPLY
(
    SELECT COALESCE(SUM(CAST(p.Amount AS int)), 0) AS PurchasedAmount
    FROM dbo.BazaarPurchaseOperation p
    WHERE p.BazaarItemId = b.BazaarItemId
) purchases
WHERE purchases.PurchasedAmount <> CAST(b.Amount AS int) - i.Amount");
                    }

                    if (status.ListingOperationAvailable == 1 &&
                        status.PriceChangeOperationAvailable == 1)
                    {
                        queries.Add(@"
SELECT CAST(2 AS tinyint) AS Severity,
       CAST(N'ACTIVE_PRICE_MISMATCH' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId,
       CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(COALESCE(latest.CompletedAtUtc, l.CompletedAtUtc) AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Current listing price differs from the latest atomic ledger price.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.BazaarListingOperation l ON l.BazaarItemId = b.BazaarItemId
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
OUTER APPLY
(
    SELECT TOP (1) p.NewUnitPrice, p.CompletedAtUtc
    FROM dbo.BazaarPriceChangeOperation p
    WHERE p.BazaarItemId = b.BazaarItemId
    ORDER BY p.CompletedAtUtc DESC
) latest
WHERE b.Price <> COALESCE(latest.NewUnitPrice, l.UnitPrice)");
                    }

                    string sql = @"
SELECT TOP (@Take) *
FROM
(
" + string.Join("\nUNION ALL\n", queries) + @"
) anomalies
ORDER BY Severity DESC, OccurredAtUtc DESC, BazaarItemId DESC;";

                    return context.Database.SqlQuery<BazaarAuditAnomalyDTO>(sql,
                            new SqlParameter("@Take", ClampTake(take)))
                        .ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to inspect bazaar anomalies.", exception);
                return Enumerable.Empty<BazaarAuditAnomalyDTO>();
            }
        }

        private IEnumerable<BazaarAuditEventDTO> LoadEvents(
            string filter,
            IEnumerable<object> parameters,
            int take)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    List<string> eventQueries = BuildEventQueries(ReadAvailability(context));
                    if (eventQueries.Count == 0) return Enumerable.Empty<BazaarAuditEventDTO>();

                    string where = string.IsNullOrWhiteSpace(filter)
                        ? string.Empty
                        : "WHERE " + filter;
                    string sql = @"
SELECT TOP (@Take) *
FROM
(
" + string.Join("\nUNION ALL\n", eventQueries) + @"
) e
" + where + @"
ORDER BY e.OccurredAtUtc DESC, e.BazaarItemId DESC;";

                    var queryParameters = parameters?.ToList() ?? new List<object>();
                    queryParameters.Add(new SqlParameter("@Take", ClampTake(take)));
                    return context.Database.SqlQuery<BazaarAuditEventDTO>(
                        sql, queryParameters.ToArray()).ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to read bazaar operation history.", exception);
                return Enumerable.Empty<BazaarAuditEventDTO>();
            }
        }

        private static List<string> BuildEventQueries(BazaarAuditStatusDTO status)
        {
            var queries = new List<string>();

            if (status.ListingOperationAvailable == 1)
            {
                queries.Add(@"
SELECT l.OperationId, CAST(1 AS tinyint) AS EventType, l.BazaarItemId,
       l.CompletedAtUtc AS OccurredAtUtc, CAST(l.SellerAccountId AS bigint) AS AccountId,
       l.SellerCharacterId AS PrimaryCharacterId,
       CAST(NULL AS bigint) AS CounterpartyCharacterId,
       l.BazaarItemInstanceId AS ItemInstanceId, l.ItemVNum,
       CAST(l.ListedAmount AS int) AS Amount, CAST(l.AmountAfter AS int) AS RemainingAmount,
       l.UnitPrice, l.UnitPrice AS PreviousUnitPrice,
       l.GoldAfter - l.GoldBefore AS GoldDelta
FROM dbo.BazaarListingOperation l");
            }

            if (status.PurchaseOperationAvailable == 1)
            {
                queries.Add(@"
SELECT p.OperationId, CAST(2 AS tinyint) AS EventType, p.BazaarItemId,
       p.CompletedAtUtc AS OccurredAtUtc, CAST(p.BuyerAccountId AS bigint) AS AccountId,
       p.BuyerCharacterId AS PrimaryCharacterId,
       CAST(p.SellerCharacterId AS bigint) AS CounterpartyCharacterId,
       p.BazaarItemInstanceId AS ItemInstanceId, p.ItemVNum,
       CAST(p.Amount AS int) AS Amount, CAST(p.BazaarAmountAfter AS int) AS RemainingAmount,
       p.UnitPrice, p.UnitPrice AS PreviousUnitPrice,
       (p.GoldAfter - p.GoldBefore) + (p.GoldBankAfter - p.GoldBankBefore) AS GoldDelta
FROM dbo.BazaarPurchaseOperation p");
            }

            if (status.PriceChangeOperationAvailable == 1)
            {
                queries.Add(@"
SELECT p.OperationId, CAST(3 AS tinyint) AS EventType, p.BazaarItemId,
       p.CompletedAtUtc AS OccurredAtUtc, CAST(p.SellerAccountId AS bigint) AS AccountId,
       p.SellerCharacterId AS PrimaryCharacterId,
       CAST(NULL AS bigint) AS CounterpartyCharacterId,
       p.BazaarItemInstanceId AS ItemInstanceId, p.ItemVNum,
       CAST(p.Amount AS int) AS Amount, CAST(p.Amount AS int) AS RemainingAmount,
       p.NewUnitPrice AS UnitPrice, p.OldUnitPrice AS PreviousUnitPrice,
       CAST(0 AS bigint) AS GoldDelta
FROM dbo.BazaarPriceChangeOperation p");
            }

            if (status.RecollectOperationAvailable == 1)
            {
                queries.Add(@"
SELECT r.OperationId, CAST(4 AS tinyint) AS EventType, r.BazaarItemId,
       r.CompletedAtUtc AS OccurredAtUtc, CAST(NULL AS bigint) AS AccountId,
       r.SellerCharacterId AS PrimaryCharacterId,
       CAST(NULL AS bigint) AS CounterpartyCharacterId,
       r.BazaarItemInstanceId AS ItemInstanceId, r.ItemVNum,
       CAST(r.SoldAmount AS int) AS Amount, CAST(r.RemainingAmount AS int) AS RemainingAmount,
       r.UnitPrice, r.UnitPrice AS PreviousUnitPrice,
       r.GoldAfter - r.GoldBefore AS GoldDelta
FROM dbo.BazaarRecollectOperation r");
            }

            return queries;
        }

        private static List<string> BuildCurrentStateAnomalyQueries()
        {
            return new List<string>
            {
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'LISTING_MISSING_ITEM' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId, CAST(NULL AS smallint) AS ItemVNum,
       CAST(b.DateStart AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Active listing references an ItemInstance row that does not exist.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE i.Id IS NULL",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'LISTING_MISSING_SELLER' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId, CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(b.DateStart AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Active listing references a seller character that does not exist.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
LEFT JOIN dbo.Character c ON c.CharacterId = b.SellerId
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE c.CharacterId IS NULL",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'LISTING_WRONG_OWNER' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(i.CharacterId AS bigint) AS CharacterId, CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(b.DateStart AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'The bazaar ItemInstance owner does not match the listing seller.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE i.CharacterId <> b.SellerId",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'LISTING_WRONG_INVENTORY' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(i.CharacterId AS bigint) AS CharacterId, CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(b.DateStart AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'The listing item is not stored in InventoryType.Bazaar.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE i.[Type] <> 9",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'LISTING_INVALID_VALUES' AS nvarchar(64)) AS Code,
       CAST(b.BazaarItemId AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(b.SellerId AS bigint) AS CharacterId, CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(b.DateStart AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Listing price, duration or item quantities are outside valid bounds.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
WHERE b.Amount <= 0 OR b.Price <= 0 OR b.Duration <= 0 OR i.Amount < 0 OR i.Amount > b.Amount",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'DUPLICATE_LISTING_ITEM' AS nvarchar(64)) AS Code,
       CAST(MIN(b.BazaarItemId) AS bigint) AS BazaarItemId,
       CAST(b.ItemInstanceId AS uniqueidentifier) AS ItemInstanceId,
       CAST(MIN(b.SellerId) AS bigint) AS CharacterId, CAST(MIN(i.ItemVNum) AS smallint) AS ItemVNum,
       CAST(MIN(b.DateStart) AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'Multiple active listings reference the same ItemInstance.' AS nvarchar(500)) AS Detail
FROM dbo.BazaarItem b
LEFT JOIN dbo.ItemInstance i ON i.Id = b.ItemInstanceId
GROUP BY b.ItemInstanceId
HAVING COUNT_BIG(*) > 1",
                @"
SELECT CAST(3 AS tinyint) AS Severity, CAST(N'ORPHAN_BAZAAR_ITEM' AS nvarchar(64)) AS Code,
       CAST(NULL AS bigint) AS BazaarItemId, CAST(i.Id AS uniqueidentifier) AS ItemInstanceId,
       CAST(i.CharacterId AS bigint) AS CharacterId, CAST(i.ItemVNum AS smallint) AS ItemVNum,
       CAST(NULL AS datetime2(3)) AS OccurredAtUtc,
       CAST(N'ItemInstance is stored as Bazaar inventory but no active listing references it.' AS nvarchar(500)) AS Detail
FROM dbo.ItemInstance i
WHERE i.[Type] = 9
  AND NOT EXISTS (SELECT 1 FROM dbo.BazaarItem b WHERE b.ItemInstanceId = i.Id)"
            };
        }

        private static BazaarAuditStatusDTO ReadAvailability(System.Data.Entity.DbContext context)
        {
            const string sql = @"
SELECT CAST(CASE WHEN OBJECT_ID(N'dbo.BazaarListingOperation', N'U') IS NULL THEN 0 ELSE 1 END AS int)
           AS ListingOperationAvailable,
       CAST(CASE WHEN OBJECT_ID(N'dbo.BazaarPurchaseOperation', N'U') IS NULL THEN 0 ELSE 1 END AS int)
           AS PurchaseOperationAvailable,
       CAST(CASE WHEN OBJECT_ID(N'dbo.BazaarPriceChangeOperation', N'U') IS NULL THEN 0 ELSE 1 END AS int)
           AS PriceChangeOperationAvailable,
       CAST(CASE WHEN OBJECT_ID(N'dbo.BazaarRecollectOperation', N'U') IS NULL THEN 0 ELSE 1 END AS int)
           AS RecollectOperationAvailable,
       CAST(0 AS bigint) AS ActiveListingCount,
       CAST(0 AS bigint) AS BazaarInventoryItemCount,
       CAST(0 AS bigint) AS ListingOperationCount,
       CAST(0 AS bigint) AS PurchaseOperationCount,
       CAST(0 AS bigint) AS PriceChangeOperationCount,
       CAST(0 AS bigint) AS RecollectOperationCount;";
            return context.Database.SqlQuery<BazaarAuditStatusDTO>(sql).Single();
        }

        private static long CountOptional(
            System.Data.Entity.DbContext context,
            string tableName,
            int available)
        {
            return available == 1 ? CountTable(context, tableName) : 0;
        }

        private static long CountTable(System.Data.Entity.DbContext context, string tableName)
        {
            return context.Database.SqlQuery<long>(
                "SELECT COUNT_BIG(*) FROM " + tableName + ";").Single();
        }

        private static int ClampTake(int take)
        {
            return Math.Max(1, Math.Min(MaximumTake, take));
        }

        private static void LogFailureOnce(string message, Exception exception)
        {
            if (Interlocked.Exchange(ref _failureLogged, 1) == 0)
            {
                Logger.Error(message, exception);
            }
        }
    }
}
