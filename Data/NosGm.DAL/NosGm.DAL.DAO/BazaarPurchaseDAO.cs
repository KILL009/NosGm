using NosGm.Core;
using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.Data;
using NosGm.Domain;
using NosGm.Mapper.Mappers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EfItemInstance = NosGm.DAL.EF.ItemInstance;

namespace NosGm.DAL.DAO
{
    /// <summary>
    /// Revalidates and commits a bazaar purchase under serializable isolation.
    /// The listing amount, buyer balances and destination inventory rows either
    /// change together or remain completely untouched.
    /// </summary>
    public sealed class BazaarPurchaseDAO
    {
        private const int MaximumItemAmount = 9999;

        public BazaarPurchaseResult Commit(BazaarPurchaseDTO request)
        {
            if (!IsRequestValid(request))
            {
                return BazaarPurchaseResult.Error;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (!HasSchema(context))
                    {
                        return BazaarPurchaseResult.MissingSchema;
                    }

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        if (AcquireLock(context, "NosGM.Bazaar.Buyer." + request.BuyerCharacterId) < 0 ||
                            AcquireLock(context, "NosGM.Bazaar.Item." + request.BazaarItemId) < 0)
                        {
                            transaction.Rollback();
                            return BazaarPurchaseResult.Error;
                        }

                        if (IsCompleted(context, request.OperationId))
                        {
                            transaction.Commit();
                            return BazaarPurchaseResult.AlreadyCommitted;
                        }

                        BazaarItem listing = context.BazaarItem
                            .FirstOrDefault(item => item.BazaarItemId == request.BazaarItemId);
                        Character buyer = context.Character
                            .FirstOrDefault(character => character.CharacterId == request.BuyerCharacterId);
                        Character seller = context.Character
                            .FirstOrDefault(character => character.CharacterId == request.SellerCharacterId);

                        if (listing == null || buyer == null || seller == null ||
                            listing.SellerId != request.SellerCharacterId ||
                            listing.ItemInstanceId != request.BazaarItemInstanceId ||
                            listing.Price != request.UnitPrice ||
                            buyer.AccountId != request.BuyerAccountId ||
                            listing.SellerId == buyer.CharacterId ||
                            seller.AccountId == buyer.AccountId ||
                            listing.DateStart.AddHours(listing.Duration) <= DateTime.Now)
                        {
                            transaction.Rollback();
                            return BazaarPurchaseResult.StateChanged;
                        }

                        EfItemInstance bazaarItem = context.ItemInstance
                            .FirstOrDefault(item => item.Id == listing.ItemInstanceId);
                        if (bazaarItem == null ||
                            bazaarItem.CharacterId != listing.SellerId ||
                            bazaarItem.Type != InventoryType.Bazaar ||
                            bazaarItem.ItemVNum != request.ItemVNum ||
                            bazaarItem.Amount != request.BazaarAmountBefore ||
                            request.Amount > bazaarItem.Amount ||
                            request.BazaarAmountAfter != bazaarItem.Amount - request.Amount ||
                            listing.IsPackage && request.Amount != bazaarItem.Amount)
                        {
                            transaction.Rollback();
                            return BazaarPurchaseResult.StateChanged;
                        }

                        long totalPrice;
                        try
                        {
                            totalPrice = checked(request.UnitPrice * request.Amount);
                        }
                        catch (OverflowException)
                        {
                            transaction.Rollback();
                            return BazaarPurchaseResult.StateChanged;
                        }

                        BazaarPurchaseResult balanceResult = ValidateBalances(buyer, request, totalPrice);
                        if (balanceResult != BazaarPurchaseResult.Success)
                        {
                            transaction.Rollback();
                            return balanceResult;
                        }

                        Dictionary<Guid, ItemInstanceDTO> beforeById = request.BuyerItemsBefore
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());
                        Dictionary<Guid, ItemInstanceDTO> afterById = request.BuyerItemsAfter
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());

                        if (!ValidateInventoryPlan(context, request, beforeById, afterById))
                        {
                            transaction.Rollback();
                            return BazaarPurchaseResult.NoInventorySpace;
                        }

                        Guid sourceSerial = bazaarItem.EquipmentSerialId ?? Guid.Empty;
                        foreach (ItemInstanceDTO after in afterById.Values)
                        {
                            bool isNew = !beforeById.ContainsKey(after.Id);
                            EfItemInstance entity = context.ItemInstance.FirstOrDefault(item => item.Id == after.Id);
                            if (isNew)
                            {
                                if (entity != null)
                                {
                                    transaction.Rollback();
                                    return BazaarPurchaseResult.StateChanged;
                                }

                                entity = new EfItemInstance();
                                context.ItemInstance.Add(entity);
                            }
                            else if (entity == null || entity.CharacterId != request.BuyerCharacterId)
                            {
                                transaction.Rollback();
                                return BazaarPurchaseResult.StateChanged;
                            }

                            ItemInstanceMapper.ToItemInstance(after, entity);
                            if (!entity.EquipmentSerialId.HasValue || entity.EquipmentSerialId == Guid.Empty)
                            {
                                transaction.Rollback();
                                return BazaarPurchaseResult.StateChanged;
                            }

                            if (isNew)
                            {
                                CloneEquipmentData(context, sourceSerial, entity.EquipmentSerialId.Value);
                            }
                        }

                        bazaarItem.Amount = request.BazaarAmountAfter;
                        buyer.Gold = request.BuyerGoldAfter;
                        buyer.GoldBank = request.BuyerGoldBankAfter;

                        context.SaveChanges();
                        InsertOperation(context, request, afterById.Keys.Except(beforeById.Keys).Count());
                        transaction.Commit();
                        return BazaarPurchaseResult.Success;
                    }
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error("BazaarPurchaseOperation table is missing. Run the bazaar purchase migration.", exception);
                return BazaarPurchaseResult.MissingSchema;
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                return BazaarPurchaseResult.AlreadyCommitted;
            }
            catch (Exception exception)
            {
                Logger.Error($"Atomic bazaar purchase failed for operation {request.OperationId}.", exception);
                return BazaarPurchaseResult.Error;
            }
        }

        private static bool IsRequestValid(BazaarPurchaseDTO request)
        {
            return request != null &&
                   request.OperationId != Guid.Empty &&
                   request.BazaarItemId > 0 &&
                   request.BuyerAccountId > 0 &&
                   request.BuyerCharacterId > 0 &&
                   request.SellerCharacterId > 0 &&
                   request.BuyerCharacterId != request.SellerCharacterId &&
                   request.BazaarItemInstanceId != Guid.Empty &&
                   request.ItemVNum > 0 &&
                   request.Amount > 0 &&
                   request.UnitPrice > 0 &&
                   request.BazaarAmountBefore >= request.Amount &&
                   request.BazaarAmountAfter >= 0;
        }

        private static BazaarPurchaseResult ValidateBalances(
            Character buyer,
            BazaarPurchaseDTO request,
            long totalPrice)
        {
            if (buyer.Gold != request.BuyerGoldBefore ||
                buyer.GoldBank != request.BuyerGoldBankBefore)
            {
                return BazaarPurchaseResult.StateChanged;
            }

            long expectedGold = buyer.Gold;
            long expectedBank = buyer.GoldBank;
            if (buyer.Gold >= totalPrice)
            {
                expectedGold -= totalPrice;
            }
            else
            {
                long bankDebit;
                try
                {
                    long remainder = totalPrice % 1000;
                    bankDebit = remainder == 0
                        ? totalPrice
                        : checked(totalPrice + (1000 - remainder));
                }
                catch (OverflowException)
                {
                    return BazaarPurchaseResult.StateChanged;
                }

                if (buyer.GoldBank < bankDebit)
                {
                    return BazaarPurchaseResult.NotEnoughGold;
                }

                expectedBank -= bankDebit;
            }

            // GoldBank is represented to the client in units of 1,000. Normalize the
            // authoritative after-state here so purchases can never create hidden bank
            // remainders that later corrupt exchange or bank packet calculations.
            request.BuyerGoldAfter = expectedGold;
            request.BuyerGoldBankAfter = expectedBank;
            return BazaarPurchaseResult.Success;
        }

        private static bool ValidateInventoryPlan(
            NosGmContext context,
            BazaarPurchaseDTO request,
            IDictionary<Guid, ItemInstanceDTO> beforeById,
            IDictionary<Guid, ItemInstanceDTO> afterById)
        {
            if (afterById.Count == 0 || beforeById.Keys.Any(id => !afterById.ContainsKey(id)))
            {
                return false;
            }

            if (beforeById.Values.Any(item => item.ItemVNum != request.ItemVNum) ||
                afterById.Values.Any(item => item.ItemVNum != request.ItemVNum ||
                    item.CharacterId != request.BuyerCharacterId ||
                    item.Type == InventoryType.Bazaar ||
                    item.Type == InventoryType.FamilyWareHouse ||
                    item.Amount <= 0 || item.Amount > MaximumItemAmount))
            {
                return false;
            }

            long amountBefore = beforeById.Values.Sum(item => (long)item.Amount);
            long amountAfter = afterById.Values.Sum(item => (long)item.Amount);
            if (amountAfter - amountBefore != request.Amount)
            {
                return false;
            }

            foreach (ItemInstanceDTO before in beforeById.Values)
            {
                EfItemInstance entity = context.ItemInstance.FirstOrDefault(item => item.Id == before.Id);
                if (!SameState(entity, before, request.BuyerCharacterId))
                {
                    return false;
                }
            }

            HashSet<Guid> affectedIds = new HashSet<Guid>(beforeById.Keys);
            var occupiedSlots = new HashSet<string>(context.ItemInstance
                .Where(item => item.CharacterId == request.BuyerCharacterId && !affectedIds.Contains(item.Id))
                .Select(item => new { item.Type, item.Slot })
                .ToList()
                .Select(item => ((int)item.Type) + ":" + item.Slot));
            var newSerials = new HashSet<Guid>();

            foreach (ItemInstanceDTO after in afterById.Values)
            {
                string slotKey = ((int)after.Type) + ":" + after.Slot;
                if (after.Slot < 0 || !occupiedSlots.Add(slotKey))
                {
                    return false;
                }

                if (beforeById.TryGetValue(after.Id, out ItemInstanceDTO before) &&
                    (before.Type != after.Type || before.Slot != after.Slot ||
                     before.EquipmentSerialId != after.EquipmentSerialId))
                {
                    return false;
                }

                if (!beforeById.ContainsKey(after.Id) &&
                    (after.EquipmentSerialId == Guid.Empty ||
                     !newSerials.Add(after.EquipmentSerialId) ||
                     context.ItemInstance.Any(item => item.Id == after.Id) ||
                     context.ItemInstance.Any(item => item.EquipmentSerialId == after.EquipmentSerialId)))
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
                   (entity.EquipmentSerialId ?? Guid.Empty) == dto.EquipmentSerialId;
        }

        private static int AcquireLock(NosGmContext context, string resource)
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

        private static bool HasSchema(NosGmContext context)
        {
            const string sql = "SELECT CASE WHEN OBJECT_ID(N'dbo.BazaarPurchaseOperation', N'U') IS NULL THEN 0 ELSE 1 END;";
            return context.Database.SqlQuery<int>(sql).Single() == 1;
        }

        private static bool IsCompleted(NosGmContext context, Guid operationId)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.BazaarPurchaseOperation WITH (UPDLOCK, HOLDLOCK)
WHERE OperationId = @OperationId;";
            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@OperationId", operationId)).Single() > 0;
        }

        private static void InsertOperation(NosGmContext context, BazaarPurchaseDTO request, int createdItemCount)
        {
            const string sql = @"
INSERT INTO dbo.BazaarPurchaseOperation
(OperationId, BazaarItemId, BuyerAccountId, BuyerCharacterId, SellerCharacterId,
 BazaarItemInstanceId, ItemVNum, Amount, UnitPrice, GoldBefore, GoldAfter,
 GoldBankBefore, GoldBankAfter, BazaarAmountBefore, BazaarAmountAfter,
 CreatedItemCount, CompletedAtUtc)
VALUES
(@OperationId, @BazaarItemId, @BuyerAccountId, @BuyerCharacterId, @SellerCharacterId,
 @BazaarItemInstanceId, @ItemVNum, @Amount, @UnitPrice, @GoldBefore, @GoldAfter,
 @GoldBankBefore, @GoldBankAfter, @BazaarAmountBefore, @BazaarAmountAfter,
 @CreatedItemCount, @CompletedAtUtc);";

            context.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@OperationId", request.OperationId),
                new SqlParameter("@BazaarItemId", request.BazaarItemId),
                new SqlParameter("@BuyerAccountId", request.BuyerAccountId),
                new SqlParameter("@BuyerCharacterId", request.BuyerCharacterId),
                new SqlParameter("@SellerCharacterId", request.SellerCharacterId),
                new SqlParameter("@BazaarItemInstanceId", request.BazaarItemInstanceId),
                new SqlParameter("@ItemVNum", request.ItemVNum),
                new SqlParameter("@Amount", request.Amount),
                new SqlParameter("@UnitPrice", request.UnitPrice),
                new SqlParameter("@GoldBefore", request.BuyerGoldBefore),
                new SqlParameter("@GoldAfter", request.BuyerGoldAfter),
                new SqlParameter("@GoldBankBefore", request.BuyerGoldBankBefore),
                new SqlParameter("@GoldBankAfter", request.BuyerGoldBankAfter),
                new SqlParameter("@BazaarAmountBefore", request.BazaarAmountBefore),
                new SqlParameter("@BazaarAmountAfter", request.BazaarAmountAfter),
                new SqlParameter("@CreatedItemCount", createdItemCount),
                new SqlParameter("@CompletedAtUtc", DateTime.UtcNow));
        }

        private static void CloneEquipmentData(NosGmContext context, Guid sourceSerial, Guid destinationSerial)
        {
            if (sourceSerial == Guid.Empty || destinationSerial == Guid.Empty || sourceSerial == destinationSerial)
            {
                return;
            }

            foreach (ShellEffect source in context.ShellEffect.Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
            {
                context.ShellEffect.Add(new ShellEffect
                {
                    Effect = source.Effect,
                    EffectLevel = source.EffectLevel,
                    EquipmentSerialId = destinationSerial,
                    IsRune = source.IsRune,
                    Type = source.Type,
                    Upgrade = source.Upgrade,
                    Value = source.Value
                });
            }

            foreach (CellonOption source in context.CellonOption.Where(option => option.EquipmentSerialId == sourceSerial).ToList())
            {
                context.CellonOption.Add(new CellonOption
                {
                    EquipmentSerialId = destinationSerial,
                    Level = source.Level,
                    Type = source.Type,
                    Value = source.Value
                });
            }

            foreach (RuneEffect source in context.RuneEffects.Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
            {
                context.RuneEffects.Add(new RuneEffect
                {
                    EquipmentSerialId = destinationSerial,
                    Type = source.Type,
                    SubType = source.SubType,
                    FirstData = source.FirstData,
                    SecondData = source.SecondData,
                    ThirdData = source.ThirdData,
                    IsPower = source.IsPower
                });
            }

            foreach (FairyEnchantment source in context.FairyEnchantment.Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
            {
                context.FairyEnchantment.Add(new FairyEnchantment
                {
                    EquipmentSerialId = destinationSerial,
                    Type = source.Type,
                    SubType = source.SubType,
                    FirstData = source.FirstData,
                    SecondData = source.SecondData,
                    ThirdData = source.ThirdData
                });
            }
        }
    }
}
