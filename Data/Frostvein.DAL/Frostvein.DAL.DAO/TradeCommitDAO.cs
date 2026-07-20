using Frostvein.Core;
using Frostvein.DAL.EF.Helpers;
using Frostvein.Data;
using Frostvein.Mapper.Mappers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EfItemInstance = Frostvein.DAL.EF.ItemInstance;

namespace Frostvein.DAL.DAO
{
    /// <summary>
    /// Persists the two characters and every affected item in one serializable SQL
    /// transaction. A completed OperationId is immutable and makes retries harmless.
    /// </summary>
    public sealed class TradeCommitDAO
    {
        public TradeCommitResult Commit(TradeCommitDTO request)
        {
            if (request == null || request.OperationId == Guid.Empty ||
                request.FirstCharacterId <= 0 || request.SecondCharacterId <= 0 ||
                request.FirstCharacterId == request.SecondCharacterId)
            {
                return TradeCommitResult.Error;
            }

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    if (!HasSchema(context))
                    {
                        return TradeCommitResult.MissingSchema;
                    }

                    using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var lockResult = AcquireOperationLock(context, request.OperationId);
                        if (lockResult < 0)
                        {
                            transaction.Rollback();
                            return TradeCommitResult.Error;
                        }

                        if (IsCompleted(context, request.OperationId))
                        {
                            transaction.Commit();
                            return TradeCommitResult.AlreadyCommitted;
                        }

                        var firstCharacter = context.Character.FirstOrDefault(c => c.CharacterId == request.FirstCharacterId);
                        var secondCharacter = context.Character.FirstOrDefault(c => c.CharacterId == request.SecondCharacterId);
                        if (firstCharacter == null || secondCharacter == null)
                        {
                            transaction.Rollback();
                            return TradeCommitResult.Conflict;
                        }

                        var beforeById = request.BeforeItems
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());
                        var afterById = request.AfterItems
                            .Where(item => item != null && item.Id != Guid.Empty)
                            .GroupBy(item => item.Id)
                            .ToDictionary(group => group.Key, group => group.First());

                        foreach (var before in beforeById.Values)
                        {
                            var entity = context.ItemInstance.FirstOrDefault(item => item.Id == before.Id);
                            if (entity == null)
                            {
                                continue;
                            }

                            if (entity.ItemVNum != before.ItemVNum ||
                                (entity.CharacterId != request.FirstCharacterId &&
                                 entity.CharacterId != request.SecondCharacterId))
                            {
                                transaction.Rollback();
                                return TradeCommitResult.Conflict;
                            }
                        }

                        foreach (var removedId in beforeById.Keys.Except(afterById.Keys).ToList())
                        {
                            var entity = context.ItemInstance.FirstOrDefault(item => item.Id == removedId);
                            if (entity != null)
                            {
                                context.ItemInstance.Remove(entity);
                            }
                        }

                        foreach (var after in afterById.Values)
                        {
                            var entity = context.ItemInstance.FirstOrDefault(item => item.Id == after.Id);
                            if (entity == null)
                            {
                                entity = new EfItemInstance();
                                context.ItemInstance.Add(entity);
                            }
                            else if (!beforeById.ContainsKey(after.Id) &&
                                     entity.CharacterId != request.FirstCharacterId &&
                                     entity.CharacterId != request.SecondCharacterId)
                            {
                                transaction.Rollback();
                                return TradeCommitResult.Conflict;
                            }

                            ItemInstanceMapper.ToItemInstance(after, entity);
                            if (!entity.EquipmentSerialId.HasValue || entity.EquipmentSerialId == Guid.Empty)
                            {
                                entity.EquipmentSerialId = Guid.NewGuid();
                                after.EquipmentSerialId = entity.EquipmentSerialId.Value;
                            }
                        }

                        firstCharacter.Gold = request.FirstGoldAfter;
                        firstCharacter.GoldBank = request.FirstGoldBankAfter;
                        secondCharacter.Gold = request.SecondGoldAfter;
                        secondCharacter.GoldBank = request.SecondGoldBankAfter;

                        context.SaveChanges();
                        InsertOperation(context, request);
                        transaction.Commit();
                        return TradeCommitResult.Success;
                    }
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                Logger.Error("TradeOperation table is missing. Run the trade migration before enabling atomic trades.", exception);
                return TradeCommitResult.MissingSchema;
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                return TradeCommitResult.AlreadyCommitted;
            }
            catch (Exception exception)
            {
                Logger.Error($"Atomic trade commit failed for operation {request.OperationId}.", exception);
                return TradeCommitResult.Error;
            }
        }

        private static int AcquireOperationLock(Frostvein.DAL.EF.FrostveinContext context, Guid operationId)
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
                new SqlParameter("@Resource", "NosGM.Trade." + operationId.ToString("N"))).Single();
        }

        private static bool HasSchema(Frostvein.DAL.EF.FrostveinContext context)
        {
            const string sql = "SELECT CASE WHEN OBJECT_ID(N'dbo.TradeOperation', N'U') IS NULL THEN 0 ELSE 1 END;";
            return context.Database.SqlQuery<int>(sql).Single() == 1;
        }

        private static bool IsCompleted(Frostvein.DAL.EF.FrostveinContext context, Guid operationId)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.TradeOperation WITH (UPDLOCK, HOLDLOCK) WHERE OperationId = @OperationId;";
            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@OperationId", operationId)).Single() > 0;
        }

        private static void InsertOperation(Frostvein.DAL.EF.FrostveinContext context, TradeCommitDTO request)
        {
            const string sql = @"
INSERT INTO dbo.TradeOperation
(OperationId, FirstCharacterId, SecondCharacterId,
 FirstGoldBefore, FirstGoldAfter, FirstGoldBankBefore, FirstGoldBankAfter,
 SecondGoldBefore, SecondGoldAfter, SecondGoldBankBefore, SecondGoldBankAfter,
 AffectedItemCount, CompletedAtUtc)
VALUES
(@OperationId, @FirstCharacterId, @SecondCharacterId,
 @FirstGoldBefore, @FirstGoldAfter, @FirstGoldBankBefore, @FirstGoldBankAfter,
 @SecondGoldBefore, @SecondGoldAfter, @SecondGoldBankBefore, @SecondGoldBankAfter,
 @AffectedItemCount, @CompletedAtUtc);";

            context.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@OperationId", request.OperationId),
                new SqlParameter("@FirstCharacterId", request.FirstCharacterId),
                new SqlParameter("@SecondCharacterId", request.SecondCharacterId),
                new SqlParameter("@FirstGoldBefore", request.FirstGoldBefore),
                new SqlParameter("@FirstGoldAfter", request.FirstGoldAfter),
                new SqlParameter("@FirstGoldBankBefore", request.FirstGoldBankBefore),
                new SqlParameter("@FirstGoldBankAfter", request.FirstGoldBankAfter),
                new SqlParameter("@SecondGoldBefore", request.SecondGoldBefore),
                new SqlParameter("@SecondGoldAfter", request.SecondGoldAfter),
                new SqlParameter("@SecondGoldBankBefore", request.SecondGoldBankBefore),
                new SqlParameter("@SecondGoldBankAfter", request.SecondGoldBankAfter),
                new SqlParameter("@AffectedItemCount", request.BeforeItems.Select(i => i.Id)
                    .Union(request.AfterItems.Select(i => i.Id)).Distinct().Count()),
                new SqlParameter("@CompletedAtUtc", DateTime.UtcNow));
        }
    }
}
