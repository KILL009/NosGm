using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Frostvein.DAL.DAO
{
    public sealed class ItemTraceDAO : IItemTraceDAO
    {
        private const int MaximumTake = 500;

        public ItemTraceDTO InsertIfMissing(ItemTraceDTO trace)
        {
            if (trace == null) throw new ArgumentNullException(nameof(trace));
            if (trace.ItemInstanceId == Guid.Empty)
                throw new ArgumentException("An item trace requires a non-empty ItemInstanceId.", nameof(trace));
            if (trace.OperationId == Guid.Empty) trace.OperationId = Guid.NewGuid();
            if (trace.Id == Guid.Empty) trace.Id = Guid.NewGuid();
            if (trace.OccurredAtUtc == default(DateTime)) trace.OccurredAtUtc = DateTime.UtcNow;

            trace.ActorName = Limit(trace.ActorName, 64);
            trace.Reason = Limit(trace.Reason, 500);
            trace.Metadata = Limit(trace.Metadata, 4000);

            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.ItemTrace WITH (UPDLOCK, HOLDLOCK)
               WHERE OperationId = @OperationId AND Sequence = @Sequence)
BEGIN
    INSERT INTO dbo.ItemTrace
    (Id, OperationId, Sequence, OccurredAtUtc, Action, Source,
     ItemInstanceId, EquipmentSerialId, ItemVNum, AmountBefore, AmountAfter,
     OwnerCharacterIdBefore, OwnerCharacterIdAfter, InventoryTypeBefore,
     InventoryTypeAfter, SlotBefore, SlotAfter, ActorAccountId,
     ActorCharacterId, ActorName, Reason, Metadata, IsSuspicious)
    VALUES
    (@Id, @OperationId, @Sequence, @OccurredAtUtc, @Action, @Source,
     @ItemInstanceId, @EquipmentSerialId, @ItemVNum, @AmountBefore, @AmountAfter,
     @OwnerCharacterIdBefore, @OwnerCharacterIdAfter, @InventoryTypeBefore,
     @InventoryTypeAfter, @SlotBefore, @SlotAfter, @ActorAccountId,
     @ActorCharacterId, @ActorName, @Reason, @Metadata, @IsSuspicious);
END";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    context.Database.ExecuteSqlCommand(sql, Parameters(trace));
                    return LoadSingle(context, trace.OperationId, trace.Sequence);
                }
            }
            catch (SqlException exception) when (exception.Number == 2601 || exception.Number == 2627)
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return LoadSingle(context, trace.OperationId, trace.Sequence);
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to append ItemTrace event.", exception);
                return null;
            }
        }

        public IEnumerable<ItemTraceDTO> LoadByItemInstanceId(Guid itemInstanceId, int take = 100)
        {
            if (itemInstanceId == Guid.Empty) return Enumerable.Empty<ItemTraceDTO>();
            return Query(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE ItemInstanceId = @Value ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", itemInstanceId));
        }

        public IEnumerable<ItemTraceDTO> LoadByEquipmentSerialId(Guid equipmentSerialId, int take = 100)
        {
            if (equipmentSerialId == Guid.Empty) return Enumerable.Empty<ItemTraceDTO>();
            return Query(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE EquipmentSerialId = @Value ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", equipmentSerialId));
        }

        public IEnumerable<ItemTraceDTO> LoadByOperationId(Guid operationId)
        {
            if (operationId == Guid.Empty) return Enumerable.Empty<ItemTraceDTO>();
            return Query(@"SELECT * FROM dbo.ItemTrace
WHERE OperationId = @Value ORDER BY Sequence ASC;", new SqlParameter("@Value", operationId));
        }

        public IEnumerable<ItemTraceDTO> LoadSuspicious(int take = 100)
        {
            return Query(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE IsSuspicious = 1 ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)));
        }

        private static ItemTraceDTO LoadSingle(FrostveinContext context, Guid operationId, int sequence)
        {
            const string sql = @"SELECT TOP (1) * FROM dbo.ItemTrace
WHERE OperationId = @OperationId AND Sequence = @Sequence;";
            return context.Database.SqlQuery<ItemTraceDTO>(sql,
                new SqlParameter("@OperationId", operationId),
                new SqlParameter("@Sequence", sequence)).FirstOrDefault();
        }

        private static IEnumerable<ItemTraceDTO> Query(string sql, params object[] parameters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<ItemTraceDTO>(sql, parameters).ToList();
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Unable to query ItemTrace history.", exception);
                return Enumerable.Empty<ItemTraceDTO>();
            }
        }

        private static object[] Parameters(ItemTraceDTO trace)
        {
            return new object[]
            {
                Parameter("@Id", trace.Id), Parameter("@OperationId", trace.OperationId),
                Parameter("@Sequence", trace.Sequence), Parameter("@OccurredAtUtc", trace.OccurredAtUtc),
                Parameter("@Action", (int)trace.Action), Parameter("@Source", (int)trace.Source),
                Parameter("@ItemInstanceId", trace.ItemInstanceId), Parameter("@EquipmentSerialId", trace.EquipmentSerialId),
                Parameter("@ItemVNum", trace.ItemVNum), Parameter("@AmountBefore", trace.AmountBefore),
                Parameter("@AmountAfter", trace.AmountAfter), Parameter("@OwnerCharacterIdBefore", trace.OwnerCharacterIdBefore),
                Parameter("@OwnerCharacterIdAfter", trace.OwnerCharacterIdAfter),
                Parameter("@InventoryTypeBefore", trace.InventoryTypeBefore.HasValue ? (int?)trace.InventoryTypeBefore.Value : null),
                Parameter("@InventoryTypeAfter", trace.InventoryTypeAfter.HasValue ? (int?)trace.InventoryTypeAfter.Value : null),
                Parameter("@SlotBefore", trace.SlotBefore), Parameter("@SlotAfter", trace.SlotAfter),
                Parameter("@ActorAccountId", trace.ActorAccountId), Parameter("@ActorCharacterId", trace.ActorCharacterId),
                Parameter("@ActorName", trace.ActorName), Parameter("@Reason", trace.Reason),
                Parameter("@Metadata", trace.Metadata), Parameter("@IsSuspicious", trace.IsSuspicious)
            };
        }

        private static SqlParameter Parameter(string name, object value) =>
            new SqlParameter(name, value ?? DBNull.Value);

        private static int ClampTake(int take) => take < 1 ? 1 : take > MaximumTake ? MaximumTake : take;

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }
    }
}
