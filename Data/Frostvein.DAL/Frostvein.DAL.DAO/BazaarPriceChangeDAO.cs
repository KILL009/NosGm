using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    /// <summary>
    /// Changes a bazaar publication price under the same SQL lock used by purchases
    /// and recollection, preventing stale price updates from racing with either path.
    /// </summary>
    public sealed class BazaarPriceChangeDAO
    {
        private const long MaximumStandardUnitPrice = 1000000;
        private const long MaximumListingValue = 1000000000;

        public BazaarPriceChangeResult Commit(BazaarPriceChangeDTO request)
        {
            if (!IsRequestValid(request))
            {
                return BazaarPriceChangeResult.Error;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (!HasSchema(context))
                    {
                        return BazaarPriceChangeResult.MissingSchema;
                    }

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        if (AcquireLock(context, "NosGM.Bazaar.Seller." + request.SellerCharacterId) < 0 ||
                            AcquireLock(context, "NosGM.Bazaar.Item." + request.BazaarItemId) < 0)
                        {
                            transaction.Rollback();
                            return BazaarPriceChangeResult.Error;
                        }

                        if (IsCompleted(context, request.OperationId))
                        {
                            transaction.Commit();
                            return BazaarPriceChangeResult.AlreadyCommitted;
                        }

                        BazaarItem listing = context.BazaarItem
                            .FirstOrDefault(item => item.BazaarItemId == request.BazaarItemId);
                        Character seller = context.Character
                            .FirstOrDefault(character => character.CharacterId == request.SellerCharacterId);

                        if (listing == null || seller == null ||
                            seller.AccountId != request.SellerAccountId ||
                            listing.SellerId != request.SellerCharacterId ||
                            listing.ItemInstanceId != request.BazaarItemInstanceId ||
                            listing.Price != request.ExpectedPrice ||
                            listing.DateStart.AddHours(listing.Duration) <= DateTime.Now)
                        {
                            transaction.Rollback();
                            return BazaarPriceChangeResult.StateChanged;
                        }

                        ItemInstance bazaarItem = context.ItemInstance
                            .FirstOrDefault(item => item.Id == listing.ItemInstanceId);
                        if (bazaarItem == null ||
                            bazaarItem.CharacterId != request.SellerCharacterId ||
                            bazaarItem.Type != InventoryType.Bazaar ||
                            bazaarItem.ItemVNum != request.ItemVNum ||
                            bazaarItem.Amount != request.Amount ||
                            listing.Amount != bazaarItem.Amount)
                        {
                            transaction.Rollback();
                            return BazaarPriceChangeResult.StateChanged;
                        }

                        if (!IsPriceValid(request.NewPrice, request.Amount, listing.MedalUsed, request.MaximumGold))
                        {
                            transaction.Rollback();
                            return BazaarPriceChangeResult.InvalidPrice;
                        }

                        listing.Price = request.NewPrice;
                        context.SaveChanges();
                        InsertOperation(context, request);
                        transaction.Commit();
                        return BazaarPriceChangeResult.Success;
                    }
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error("BazaarPriceChangeOperation table is missing. Run the bazaar price migration.", exception);
                return BazaarPriceChangeResult.MissingSchema;
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                return BazaarPriceChangeResult.AlreadyCommitted;
            }
            catch (Exception exception)
            {
                Logger.Error($"Atomic bazaar price change failed for operation {request.OperationId}.", exception);
                return BazaarPriceChangeResult.Error;
            }
        }

        private static bool IsRequestValid(BazaarPriceChangeDTO request)
        {
            return request != null &&
                   request.OperationId != Guid.Empty &&
                   request.BazaarItemId > 0 &&
                   request.SellerAccountId > 0 &&
                   request.SellerCharacterId > 0 &&
                   request.BazaarItemInstanceId != Guid.Empty &&
                   request.ItemVNum > 0 &&
                   request.Amount > 0 &&
                   request.ExpectedPrice > 0 &&
                   request.NewPrice > 0 &&
                   request.MaximumGold > 0;
        }

        private static bool IsPriceValid(long price, short amount, bool medalUsed, long maximumGold)
        {
            long total;
            try
            {
                total = checked(price * amount);
            }
            catch (OverflowException)
            {
                return false;
            }

            long unitLimit = medalUsed ? maximumGold : MaximumStandardUnitPrice;
            long totalLimit = Math.Min(MaximumListingValue, maximumGold);
            return price > 0 &&
                   price < unitLimit &&
                   total > 0 &&
                   total <= totalLimit;
        }

        private static int AcquireLock(FrostveinContext context, string resource)
        {
            const string sql = @"
DECLARE @Result int;
EXEC @Result = sys.sp_getapplock
    @Resource = @Resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;
SELECT @Result;";

            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@Resource", resource)).Single();
        }

        private static bool HasSchema(FrostveinContext context)
        {
            const string sql =
                "SELECT CASE WHEN OBJECT_ID(N'dbo.BazaarPriceChangeOperation', N'U') IS NULL THEN 0 ELSE 1 END;";
            return context.Database.SqlQuery<int>(sql).Single() == 1;
        }

        private static bool IsCompleted(FrostveinContext context, Guid operationId)
        {
            const string sql = @"SELECT COUNT(1)
FROM dbo.BazaarPriceChangeOperation WITH (UPDLOCK, HOLDLOCK)
WHERE OperationId = @OperationId;";

            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@OperationId", operationId)).Single() > 0;
        }

        private static void InsertOperation(FrostveinContext context, BazaarPriceChangeDTO request)
        {
            const string sql = @"
INSERT INTO dbo.BazaarPriceChangeOperation
(OperationId, BazaarItemId, SellerAccountId, SellerCharacterId, BazaarItemInstanceId,
 ItemVNum, Amount, OldUnitPrice, NewUnitPrice, CompletedAtUtc)
VALUES
(@OperationId, @BazaarItemId, @SellerAccountId, @SellerCharacterId, @BazaarItemInstanceId,
 @ItemVNum, @Amount, @OldUnitPrice, @NewUnitPrice, @CompletedAtUtc);";

            context.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@OperationId", request.OperationId),
                new SqlParameter("@BazaarItemId", request.BazaarItemId),
                new SqlParameter("@SellerAccountId", request.SellerAccountId),
                new SqlParameter("@SellerCharacterId", request.SellerCharacterId),
                new SqlParameter("@BazaarItemInstanceId", request.BazaarItemInstanceId),
                new SqlParameter("@ItemVNum", request.ItemVNum),
                new SqlParameter("@Amount", request.Amount),
                new SqlParameter("@OldUnitPrice", request.ExpectedPrice),
                new SqlParameter("@NewUnitPrice", request.NewPrice),
                new SqlParameter("@CompletedAtUtc", DateTime.UtcNow));
        }
    }
}
