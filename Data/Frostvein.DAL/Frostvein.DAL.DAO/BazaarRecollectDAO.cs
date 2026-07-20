using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EfItemInstance = Frostvein.DAL.EF.ItemInstance;

namespace Frostvein.DAL.DAO
{
    /// <summary>
    /// Removes a bazaar listing, credits its seller and restores the remaining item
    /// inside one serializable transaction. It shares the listing application lock
    /// used by purchases, closing the buy-versus-recollect duplication window.
    /// </summary>
    public sealed class BazaarRecollectDAO
    {
        private const int MaximumItemAmount = 9999;

        public BazaarRecollectResult Commit(BazaarRecollectDTO request)
        {
            if (!IsRequestValid(request))
            {
                return BazaarRecollectResult.Error;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (!HasSchema(context))
                    {
                        return BazaarRecollectResult.MissingSchema;
                    }

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        if (AcquireLock(context, "NosGM.Bazaar.Item." + request.BazaarItemId) < 0 ||
                            AcquireLock(context, "NosGM.Bazaar.Seller." + request.SellerCharacterId) < 0)
                        {
                            transaction.Rollback();
                            return BazaarRecollectResult.Error;
                        }

                        if (IsCompleted(context, request.OperationId))
                        {
                            transaction.Commit();
                            return BazaarRecollectResult.AlreadyCommitted;
                        }

                        BazaarItem listing = context.BazaarItem
                            .FirstOrDefault(item => item.BazaarItemId == request.BazaarItemId);
                        Character seller = context.Character
                            .FirstOrDefault(character => character.CharacterId == request.SellerCharacterId);
                        EfItemInstance source = context.ItemInstance
                            .FirstOrDefault(item => item.Id == request.BazaarItemInstanceId);

                        if (listing == null || seller == null || source == null ||
                            listing.SellerId != request.SellerCharacterId ||
                            listing.ItemInstanceId != request.BazaarItemInstanceId ||
                            listing.Price != request.UnitPrice ||
                            listing.Amount != request.ListingAmount ||
                            source.CharacterId != request.SellerCharacterId ||
                            source.Type != InventoryType.Bazaar ||
                            source.ItemVNum != request.ItemVNum ||
                            source.Amount != request.RemainingAmount ||
                            listing.DateStart.AddHours(listing.Duration)
                                .AddDays(listing.MedalUsed ? 30 : 7) <= DateTime.Now)
                        {
                            transaction.Rollback();
                            return BazaarRecollectResult.StateChanged;
                        }

                        short soldAmount = (short)(listing.Amount - source.Amount);
                        if (soldAmount < 0 || soldAmount != request.SoldAmount)
                        {
                            transaction.Rollback();
                            return BazaarRecollectResult.StateChanged;
                        }

                        long gross;
                        try
                        {
                            gross = checked(listing.Price * soldAmount);
                        }
                        catch (OverflowException)
                        {
                            transaction.Rollback();
                            return BazaarRecollectResult.StateChanged;
                        }

                        long tax = listing.MedalUsed ? 0 : gross / 10;
                        long proceeds = gross - tax;
                        long goldBefore = seller.Gold;
                        long goldAfter;
                        try
                        {
                            goldAfter = checked(goldBefore + proceeds);
                        }
                        catch (OverflowException)
                        {
                            transaction.Rollback();
                            return BazaarRecollectResult.GoldLimit;
                        }

                        // SQL is authoritative for the seller balance and payout. The live World
                        // session may be stale after another bazaar operation or a reconnect.
                        // Normalize the mutable plan so the caller applies the exact committed values.
                        request.Tax = tax;
                        request.Proceeds = proceeds;
                        request.GoldBefore = goldBefore;
                        request.GoldAfter = goldAfter;

                        Dictionary<Guid, ItemInstanceDTO> beforeById = request.ItemsBefore
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());
                        Dictionary<Guid, ItemInstanceDTO> afterById = request.ItemsAfter
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());

                        if (request.RemainingAmount == 0)
                        {
                            if (!ValidateFullySoldPlan(request))
                            {
                                transaction.Rollback();
                                return BazaarRecollectResult.StateChanged;
                            }

                            // A completely sold listing restores no item and therefore requires no
                            // inventory slot. The SQL row and sold amount were already validated under
                            // the listing lock, so transport-only inventory snapshots are irrelevant.
                            context.ItemInstance.Remove(source);
                        }
                        else
                        {
                            if (!ValidateInventoryPlan(context, request, beforeById, afterById))
                            {
                                transaction.Rollback();
                                return BazaarRecollectResult.NoInventorySpace;
                            }

                            foreach (Guid removedId in beforeById.Keys.Except(afterById.Keys).ToList())
                            {
                                EfItemInstance entity = context.ItemInstance.FirstOrDefault(item => item.Id == removedId);
                                if (entity != null)
                                {
                                    context.ItemInstance.Remove(entity);
                                }
                            }

                            foreach (ItemInstanceDTO after in afterById.Values)
                            {
                                EfItemInstance entity = context.ItemInstance.FirstOrDefault(item => item.Id == after.Id);
                                if (entity == null || entity.CharacterId != request.SellerCharacterId)
                                {
                                    transaction.Rollback();
                                    return BazaarRecollectResult.StateChanged;
                                }

                                ItemInstanceMapper.ToItemInstance(after, entity);
                            }
                        }

                        context.BazaarItem.Remove(listing);
                        seller.Gold = goldAfter;
                        context.SaveChanges();
                        InsertOperation(context, request);
                        transaction.Commit();
                        return BazaarRecollectResult.Success;
                    }
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error("BazaarRecollectOperation table is missing. Run the bazaar recollection migration.", exception);
                return BazaarRecollectResult.MissingSchema;
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                return BazaarRecollectResult.AlreadyCommitted;
            }
            catch (Exception exception)
            {
                Logger.Error($"Atomic bazaar recollection failed for operation {request.OperationId}.", exception);
                return BazaarRecollectResult.Error;
            }
        }

        private static bool IsRequestValid(BazaarRecollectDTO request)
        {
            return request != null &&
                   request.OperationId != Guid.Empty &&
                   request.BazaarItemId > 0 &&
                   request.SellerCharacterId > 0 &&
                   request.BazaarItemInstanceId != Guid.Empty &&
                   request.ItemVNum > 0 &&
                   request.ListingAmount >= 0 &&
                   request.RemainingAmount >= 0 &&
                   request.SoldAmount >= 0 &&
                   request.UnitPrice > 0 &&
                   request.Tax >= 0 &&
                   request.Proceeds >= 0 &&
                   request.GoldBefore >= 0 &&
                   request.GoldAfter >= request.GoldBefore;
        }

        private static bool ValidateFullySoldPlan(BazaarRecollectDTO request)
        {
            return request.RemainingAmount == 0 &&
                   request.SoldAmount == request.ListingAmount;
        }

        private static bool ValidateInventoryPlan(
            FrostveinContext context,
            BazaarRecollectDTO request,
            IDictionary<Guid, ItemInstanceDTO> beforeById,
            IDictionary<Guid, ItemInstanceDTO> afterById)
        {
            if (!beforeById.TryGetValue(request.BazaarItemInstanceId, out ItemInstanceDTO sourceBefore) ||
                sourceBefore.CharacterId != request.SellerCharacterId ||
                sourceBefore.ItemVNum != request.ItemVNum ||
                sourceBefore.Type != InventoryType.Bazaar ||
                sourceBefore.Amount != request.RemainingAmount ||
                afterById.Keys.Any(id => !beforeById.ContainsKey(id)))
            {
                return false;
            }

            if (beforeById.Values.Any(item => item.CharacterId != request.SellerCharacterId ||
                                              item.ItemVNum != request.ItemVNum) ||
                afterById.Values.Any(item => item.CharacterId != request.SellerCharacterId ||
                                             item.ItemVNum != request.ItemVNum ||
                                             item.Type == InventoryType.Bazaar ||
                                             item.Type == InventoryType.FamilyWareHouse ||
                                             item.Amount <= 0 || item.Amount > MaximumItemAmount))
            {
                return false;
            }

            if (beforeById.Values.Sum(item => (long)item.Amount) !=
                afterById.Values.Sum(item => (long)item.Amount))
            {
                return false;
            }

            foreach (ItemInstanceDTO before in beforeById.Values)
            {
                EfItemInstance entity = context.ItemInstance.FirstOrDefault(item => item.Id == before.Id);
                if (!SameState(entity, before, request.SellerCharacterId))
                {
                    return false;
                }
            }

            HashSet<Guid> affectedIds = new HashSet<Guid>(beforeById.Keys);
            var occupiedSlots = new HashSet<string>(context.ItemInstance
                .Where(item => item.CharacterId == request.SellerCharacterId && !affectedIds.Contains(item.Id))
                .Select(item => new { item.Type, item.Slot })
                .ToList()
                .Select(item => ((int)item.Type) + ":" + item.Slot));

            foreach (ItemInstanceDTO after in afterById.Values)
            {
                string slotKey = ((int)after.Type) + ":" + after.Slot;
                if (after.Slot < 0 || !occupiedSlots.Add(slotKey))
                {
                    return false;
                }

                ItemInstanceDTO before = beforeById[after.Id];
                if (before.Id != request.BazaarItemInstanceId &&
                    (before.Type != after.Type || before.Slot != after.Slot ||
                     !SerialsCompatible(before.EquipmentSerialId, after.EquipmentSerialId) ||
                     after.Amount < before.Amount))
                {
                    return false;
                }

                if (before.Id == request.BazaarItemInstanceId &&
                    !SerialsCompatible(before.EquipmentSerialId, after.EquipmentSerialId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameState(EfItemInstance entity, ItemInstanceDTO dto, long ownerCharacterId)
        {
            return entity != null &&
                   entity.CharacterId == ownerCharacterId &&
                   entity.ItemVNum == dto.ItemVNum &&
                   entity.Amount == dto.Amount &&
                   entity.Type == dto.Type &&
                   entity.Slot == dto.Slot &&
                   SerialsCompatible(entity.EquipmentSerialId ?? Guid.Empty, dto.EquipmentSerialId);
        }

        private static bool SerialsCompatible(Guid storedSerial, Guid transportedSerial)
        {
            return storedSerial == Guid.Empty ||
                   transportedSerial == Guid.Empty ||
                   storedSerial == transportedSerial;
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
            const string sql = "SELECT CASE WHEN OBJECT_ID(N'dbo.BazaarRecollectOperation', N'U') IS NULL THEN 0 ELSE 1 END;";
            return context.Database.SqlQuery<int>(sql).Single() == 1;
        }

        private static bool IsCompleted(FrostveinContext context, Guid operationId)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.BazaarRecollectOperation WITH (UPDLOCK, HOLDLOCK)
WHERE OperationId = @OperationId;";
            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@OperationId", operationId)).Single() > 0;
        }

        private static void InsertOperation(FrostveinContext context, BazaarRecollectDTO request)
        {
            const string sql = @"
INSERT INTO dbo.BazaarRecollectOperation
(OperationId, BazaarItemId, SellerCharacterId, BazaarItemInstanceId,
 ItemVNum, ListingAmount, RemainingAmount, SoldAmount, UnitPrice,
 Tax, Proceeds, GoldBefore, GoldAfter, CompletedAtUtc)
VALUES
(@OperationId, @BazaarItemId, @SellerCharacterId, @BazaarItemInstanceId,
 @ItemVNum, @ListingAmount, @RemainingAmount, @SoldAmount, @UnitPrice,
 @Tax, @Proceeds, @GoldBefore, @GoldAfter, @CompletedAtUtc);";

            context.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@OperationId", request.OperationId),
                new SqlParameter("@BazaarItemId", request.BazaarItemId),
                new SqlParameter("@SellerCharacterId", request.SellerCharacterId),
                new SqlParameter("@BazaarItemInstanceId", request.BazaarItemInstanceId),
                new SqlParameter("@ItemVNum", request.ItemVNum),
                new SqlParameter("@ListingAmount", request.ListingAmount),
                new SqlParameter("@RemainingAmount", request.RemainingAmount),
                new SqlParameter("@SoldAmount", request.SoldAmount),
                new SqlParameter("@UnitPrice", request.UnitPrice),
                new SqlParameter("@Tax", request.Tax),
                new SqlParameter("@Proceeds", request.Proceeds),
                new SqlParameter("@GoldBefore", request.GoldBefore),
                new SqlParameter("@GoldAfter", request.GoldAfter),
                new SqlParameter("@CompletedAtUtc", DateTime.UtcNow));
        }
    }
}
