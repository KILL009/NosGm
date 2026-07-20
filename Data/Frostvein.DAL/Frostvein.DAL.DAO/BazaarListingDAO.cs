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
    /// Moves an inventory item into the bazaar and creates its publication in one
    /// serializable transaction. Gold, source amount, bazaar item and publication
    /// either commit together or remain untouched.
    /// </summary>
    public sealed class BazaarListingDAO
    {
        private const int MaximumBazaarSlots = 9999;
        private const long MaximumStandardUnitPrice = 1000000;
        private const int StandardListingLimit = 20;
        private const int MedalListingLimit = 100;

        public BazaarListingResult Commit(BazaarListingDTO request)
        {
            if (!IsRequestValid(request))
            {
                return BazaarListingResult.Error;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (!HasSchema(context))
                    {
                        return BazaarListingResult.MissingSchema;
                    }

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        if (AcquireLock(context, "NosGM.Bazaar.Seller." + request.SellerCharacterId) < 0 ||
                            AcquireLock(context, "NosGM.Item." + request.SourceBefore.Id) < 0)
                        {
                            transaction.Rollback();
                            return BazaarListingResult.Error;
                        }

                        long completedListingId = LoadCompletedListingId(context, request.OperationId);
                        if (completedListingId > 0)
                        {
                            request.Listing.BazaarItemId = completedListingId;
                            transaction.Commit();
                            return BazaarListingResult.AlreadyCommitted;
                        }

                        Character seller = context.Character
                            .FirstOrDefault(character => character.CharacterId == request.SellerCharacterId);
                        EfItemInstance source = context.ItemInstance
                            .FirstOrDefault(item => item.Id == request.SourceBefore.Id);
                        Item itemDefinition = context.Item
                            .FirstOrDefault(item => item.VNum == request.SourceBefore.ItemVNum);

                        if (seller == null || source == null || itemDefinition == null ||
                            seller.AccountId != request.SellerAccountId ||
                            seller.Gold != request.GoldBefore ||
                            !SameState(source, request.SourceBefore, request.SellerCharacterId))
                        {
                            transaction.Rollback();
                            return BazaarListingResult.StateChanged;
                        }

                        if (!IsTradableSource(source, itemDefinition))
                        {
                            transaction.Rollback();
                            return BazaarListingResult.InvalidItem;
                        }

                        bool hasMedal = context.StaticBonus.Any(bonus =>
                            bonus.CharacterId == request.SellerCharacterId &&
                            bonus.DateEnd > DateTime.Now &&
                            (bonus.StaticBonusType == StaticBonusType.BazaarMedalGold ||
                             bonus.StaticBonusType == StaticBonusType.BazaarMedalSilver));

                        int listingLimit = hasMedal ? MedalListingLimit : StandardListingLimit;
                        if (context.BazaarItem.Count(listing => listing.SellerId == request.SellerCharacterId) >=
                            listingLimit)
                        {
                            transaction.Rollback();
                            return BazaarListingResult.ListingLimitReached;
                        }

                        BazaarListingResult priceResult = ValidatePriceAndTax(request, hasMedal);
                        if (priceResult != BazaarListingResult.Success)
                        {
                            transaction.Rollback();
                            return priceResult;
                        }

                        if (!ValidateInventoryPlan(request, source))
                        {
                            transaction.Rollback();
                            return BazaarListingResult.StateChanged;
                        }

                        short? bazaarSlot = FindFreeBazaarSlot(context, request.SellerCharacterId);
                        if (!bazaarSlot.HasValue)
                        {
                            transaction.Rollback();
                            return BazaarListingResult.ListingLimitReached;
                        }

                        request.BazaarItemAfter.CharacterId = request.SellerCharacterId;
                        request.BazaarItemAfter.Type = InventoryType.Bazaar;
                        request.BazaarItemAfter.Slot = bazaarSlot.Value;

                        bool fullTransfer = request.SourceAfter == null;
                        Guid sourceSerial = source.EquipmentSerialId ?? Guid.Empty;

                        if (fullTransfer)
                        {
                            ItemInstanceMapper.ToItemInstance(request.BazaarItemAfter, source);
                        }
                        else
                        {
                            ItemInstanceMapper.ToItemInstance(request.SourceAfter, source);

                            if (context.ItemInstance.Any(item => item.Id == request.BazaarItemAfter.Id) ||
                                (request.BazaarItemAfter.EquipmentSerialId != Guid.Empty &&
                                 context.ItemInstance.Any(item =>
                                     item.EquipmentSerialId == request.BazaarItemAfter.EquipmentSerialId)))
                            {
                                transaction.Rollback();
                                return BazaarListingResult.StateChanged;
                            }

                            var bazaarEntity = new EfItemInstance();
                            ItemInstanceMapper.ToItemInstance(request.BazaarItemAfter, bazaarEntity);
                            context.ItemInstance.Add(bazaarEntity);
                            CloneEquipmentData(
                                context,
                                sourceSerial,
                                request.BazaarItemAfter.EquipmentSerialId);
                        }

                        request.Listing.AccountId = request.SellerAccountId;
                        request.Listing.SellerId = request.SellerCharacterId;
                        request.Listing.ItemInstanceId = request.BazaarItemAfter.Id;
                        request.Listing.Amount = request.BazaarItemAfter.Amount;
                        request.Listing.MedalUsed = hasMedal;
                        request.Listing.DateStart = DateTime.Now;

                        var listingEntity = new BazaarItem
                        {
                            AccountId = request.Listing.AccountId,
                            RegistrationIP = request.Listing.RegistrationIP,
                            CurrentIp = request.Listing.CurrentIp,
                            Amount = request.Listing.Amount,
                            DateStart = request.Listing.DateStart,
                            Duration = request.Listing.Duration,
                            IsPackage = request.Listing.IsPackage,
                            ItemInstanceId = request.Listing.ItemInstanceId,
                            MedalUsed = request.Listing.MedalUsed,
                            Price = request.Listing.Price,
                            SellerId = request.Listing.SellerId
                        };
                        context.BazaarItem.Add(listingEntity);

                        seller.Gold = request.GoldAfter;
                        context.SaveChanges();

                        request.Listing.BazaarItemId = listingEntity.BazaarItemId;
                        InsertOperation(context, request, fullTransfer);
                        transaction.Commit();
                        return BazaarListingResult.Success;
                    }
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error("BazaarListingOperation table is missing. Run the bazaar listing migration.", exception);
                return BazaarListingResult.MissingSchema;
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                return BazaarListingResult.AlreadyCommitted;
            }
            catch (Exception exception)
            {
                Logger.Error($"Atomic bazaar listing failed for operation {request.OperationId}.", exception);
                return BazaarListingResult.Error;
            }
        }

        private static bool IsRequestValid(BazaarListingDTO request)
        {
            return request != null &&
                   request.OperationId != Guid.Empty &&
                   request.SellerAccountId > 0 &&
                   request.SellerCharacterId > 0 &&
                   request.MaximumGold > 0 &&
                   request.SourceBefore != null &&
                   request.SourceBefore.Id != Guid.Empty &&
                   request.SourceBefore.ItemVNum > 0 &&
                   request.SourceBefore.Amount > 0 &&
                   request.BazaarItemAfter != null &&
                   request.BazaarItemAfter.Id != Guid.Empty &&
                   request.BazaarItemAfter.Amount > 0 &&
                   request.Listing != null &&
                   request.Listing.Price > 0 &&
                   IsSupportedDuration(request.Listing.Duration);
        }

        private static bool IsSupportedDuration(short duration) =>
            duration == 24 || duration == 168 || duration == 360 || duration == 720;

        private static bool SameState(EfItemInstance entity, ItemInstanceDTO dto, long ownerCharacterId)
        {
            return entity != null &&
                   entity.CharacterId == ownerCharacterId &&
                   entity.ItemVNum == dto.ItemVNum &&
                   entity.Amount == dto.Amount &&
                   entity.Type == dto.Type &&
                   entity.Slot == dto.Slot &&
                   (entity.EquipmentSerialId ?? Guid.Empty) == dto.EquipmentSerialId &&
                   entity.BoundCharacterId == dto.BoundCharacterId &&
                   entity.ItemDeleteTime == dto.ItemDeleteTime;
        }

        private static bool IsTradableSource(EfItemInstance source, Item itemDefinition)
        {
            if (source.Type != InventoryType.Equipment &&
                source.Type != InventoryType.Main &&
                source.Type != InventoryType.Etc)
            {
                return false;
            }

            bool isBound = source.BoundCharacterId.HasValue &&
                           itemDefinition.ItemType != (byte)ItemType.Armor &&
                           itemDefinition.ItemType != (byte)ItemType.Weapon;

            return itemDefinition.IsSoldable &&
                   itemDefinition.IsTradable &&
                   !isBound &&
                   source.ItemDeleteTime == null &&
                   source.Amount > 0;
        }

        private static BazaarListingResult ValidatePriceAndTax(BazaarListingDTO request, bool hasMedal)
        {
            long totalPrice;
            try
            {
                totalPrice = checked(request.Listing.Price * request.BazaarItemAfter.Amount);
            }
            catch (OverflowException)
            {
                return BazaarListingResult.InvalidPrice;
            }

            long unitLimit = hasMedal ? request.MaximumGold : MaximumStandardUnitPrice;
            if (request.Listing.Price <= 0 ||
                request.Listing.Price >= unitLimit ||
                totalPrice <= 0 ||
                totalPrice >= request.MaximumGold)
            {
                return BazaarListingResult.InvalidPrice;
            }

            long standardTax = totalPrice > 100000 ? totalPrice / 200 : 500;
            long medalTax = totalPrice >= 4000
                ? Math.Min(10000, 60 + ((totalPrice - 4000) / 2000 * 30))
                : 50;
            long expectedTax = hasMedal ? medalTax : standardTax;

            if (request.GoldBefore < expectedTax)
            {
                return BazaarListingResult.NotEnoughGold;
            }

            request.Tax = expectedTax;
            request.GoldAfter = request.GoldBefore - expectedTax;
            return BazaarListingResult.Success;
        }

        private static bool ValidateInventoryPlan(BazaarListingDTO request, EfItemInstance source)
        {
            ItemInstanceDTO before = request.SourceBefore;
            ItemInstanceDTO sourceAfter = request.SourceAfter;
            ItemInstanceDTO bazaarAfter = request.BazaarItemAfter;

            if (bazaarAfter.CharacterId != request.SellerCharacterId ||
                bazaarAfter.ItemVNum != before.ItemVNum ||
                bazaarAfter.Amount <= 0 ||
                bazaarAfter.Amount > before.Amount)
            {
                return false;
            }

            bool fullTransfer = bazaarAfter.Amount == before.Amount;
            if (source.Type == InventoryType.Equipment && !fullTransfer)
            {
                return false;
            }

            if (fullTransfer)
            {
                return sourceAfter == null &&
                       bazaarAfter.Id == before.Id &&
                       bazaarAfter.EquipmentSerialId == before.EquipmentSerialId;
            }

            return sourceAfter != null &&
                   sourceAfter.Id == before.Id &&
                   sourceAfter.CharacterId == before.CharacterId &&
                   sourceAfter.ItemVNum == before.ItemVNum &&
                   sourceAfter.Type == before.Type &&
                   sourceAfter.Slot == before.Slot &&
                   sourceAfter.EquipmentSerialId == before.EquipmentSerialId &&
                   sourceAfter.Amount == before.Amount - bazaarAfter.Amount &&
                   bazaarAfter.Id != before.Id &&
                   bazaarAfter.EquipmentSerialId != before.EquipmentSerialId;
        }

        private static short? FindFreeBazaarSlot(FrostveinContext context, long sellerCharacterId)
        {
            var occupied = new HashSet<short>(context.ItemInstance
                .Where(item => item.CharacterId == sellerCharacterId && item.Type == InventoryType.Bazaar)
                .Select(item => item.Slot)
                .ToList());

            for (short slot = 0; slot < MaximumBazaarSlots; slot++)
            {
                if (!occupied.Contains(slot))
                {
                    return slot;
                }
            }

            return null;
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
                "SELECT CASE WHEN OBJECT_ID(N'dbo.BazaarListingOperation', N'U') IS NULL THEN 0 ELSE 1 END;";
            return context.Database.SqlQuery<int>(sql).Single() == 1;
        }

        private static long LoadCompletedListingId(FrostveinContext context, Guid operationId)
        {
            const string sql = @"SELECT BazaarItemId
FROM dbo.BazaarListingOperation WITH (UPDLOCK, HOLDLOCK)
WHERE OperationId = @OperationId;";

            return context.Database.SqlQuery<long>(sql,
                new SqlParameter("@OperationId", operationId)).SingleOrDefault();
        }

        private static void InsertOperation(FrostveinContext context, BazaarListingDTO request, bool fullTransfer)
        {
            const string sql = @"
INSERT INTO dbo.BazaarListingOperation
(OperationId, BazaarItemId, SellerAccountId, SellerCharacterId, SourceItemInstanceId,
 BazaarItemInstanceId, ItemVNum, AmountBefore, ListedAmount, AmountAfter,
 UnitPrice, Tax, GoldBefore, GoldAfter, FullTransfer, CompletedAtUtc)
VALUES
(@OperationId, @BazaarItemId, @SellerAccountId, @SellerCharacterId, @SourceItemInstanceId,
 @BazaarItemInstanceId, @ItemVNum, @AmountBefore, @ListedAmount, @AmountAfter,
 @UnitPrice, @Tax, @GoldBefore, @GoldAfter, @FullTransfer, @CompletedAtUtc);";

            context.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@OperationId", request.OperationId),
                new SqlParameter("@BazaarItemId", request.Listing.BazaarItemId),
                new SqlParameter("@SellerAccountId", request.SellerAccountId),
                new SqlParameter("@SellerCharacterId", request.SellerCharacterId),
                new SqlParameter("@SourceItemInstanceId", request.SourceBefore.Id),
                new SqlParameter("@BazaarItemInstanceId", request.BazaarItemAfter.Id),
                new SqlParameter("@ItemVNum", request.SourceBefore.ItemVNum),
                new SqlParameter("@AmountBefore", request.SourceBefore.Amount),
                new SqlParameter("@ListedAmount", request.BazaarItemAfter.Amount),
                new SqlParameter("@AmountAfter",
                    request.SourceAfter == null ? 0 : request.SourceAfter.Amount),
                new SqlParameter("@UnitPrice", request.Listing.Price),
                new SqlParameter("@Tax", request.Tax),
                new SqlParameter("@GoldBefore", request.GoldBefore),
                new SqlParameter("@GoldAfter", request.GoldAfter),
                new SqlParameter("@FullTransfer", fullTransfer),
                new SqlParameter("@CompletedAtUtc", DateTime.UtcNow));
        }

        private static void CloneEquipmentData(FrostveinContext context, Guid sourceSerial, Guid destinationSerial)
        {
            if (sourceSerial == Guid.Empty || destinationSerial == Guid.Empty || sourceSerial == destinationSerial)
            {
                return;
            }

            foreach (ShellEffect source in context.ShellEffect
                         .Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
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

            foreach (CellonOption source in context.CellonOption
                         .Where(option => option.EquipmentSerialId == sourceSerial).ToList())
            {
                context.CellonOption.Add(new CellonOption
                {
                    EquipmentSerialId = destinationSerial,
                    Level = source.Level,
                    Type = source.Type,
                    Value = source.Value
                });
            }

            foreach (RuneEffect source in context.RuneEffects
                         .Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
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

            foreach (FairyEnchantment source in context.FairyEnchantment
                         .Where(effect => effect.EquipmentSerialId == sourceSerial).ToList())
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
