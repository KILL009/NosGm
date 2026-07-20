using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

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
            return Query<ItemTraceDTO>(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE ItemInstanceId = @Value ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", itemInstanceId));
        }

        public IEnumerable<ItemTraceDTO> LoadByEquipmentSerialId(Guid equipmentSerialId, int take = 100)
        {
            if (equipmentSerialId == Guid.Empty) return Enumerable.Empty<ItemTraceDTO>();
            return Query<ItemTraceDTO>(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE EquipmentSerialId = @Value ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", equipmentSerialId));
        }

        public IEnumerable<ItemTraceDTO> LoadByOperationId(Guid operationId)
        {
            if (operationId == Guid.Empty) return Enumerable.Empty<ItemTraceDTO>();
            return Query<ItemTraceDTO>(@"SELECT * FROM dbo.ItemTrace
WHERE OperationId = @Value ORDER BY Sequence ASC;", new SqlParameter("@Value", operationId));
        }

        public IEnumerable<ItemTraceDTO> LoadSuspicious(int take = 100) =>
            Query<ItemTraceDTO>(@"SELECT TOP (@Take) * FROM dbo.ItemTrace
WHERE IsSuspicious = 1 ORDER BY OccurredAtUtc DESC, Sequence DESC;",
                new SqlParameter("@Take", ClampTake(take)));

        public IEnumerable<DuplicateEquipmentSerialItemDTO> LoadCurrentItemsByEquipmentSerialId(
            Guid equipmentSerialId,
            int take = 100)
        {
            if (equipmentSerialId == Guid.Empty)
                return Enumerable.Empty<DuplicateEquipmentSerialItemDTO>();

            const string sql = @"
SELECT TOP (@Take)
       i.EquipmentSerialId,
       COUNT(*) OVER (PARTITION BY i.EquipmentSerialId) AS InstanceCount,
       i.Id AS ItemInstanceId,
       i.ItemVNum,
       i.Amount,
       i.CharacterId,
       CAST(i.Type AS int) AS InventoryTypeValue,
       i.Slot,
       i.Rare,
       i.Upgrade
FROM dbo.ItemInstance i
WHERE i.EquipmentSerialId = @EquipmentSerialId
ORDER BY i.CharacterId, i.Type, i.Slot, i.Id;";

            return Query<DuplicateEquipmentSerialItemDTO>(sql,
                new SqlParameter("@Take", ClampTake(take)),
                new SqlParameter("@EquipmentSerialId", equipmentSerialId));
        }

        public IEnumerable<DuplicateEquipmentSerialItemDTO> LoadDuplicateEquipmentSerialItems(int takeGroups = 20)
        {
            const string sql = @"
WITH DuplicateSerials AS
(
    SELECT TOP (@TakeGroups)
           EquipmentSerialId,
           COUNT(*) AS InstanceCount
    FROM dbo.ItemInstance
    WHERE EquipmentSerialId IS NOT NULL
      AND EquipmentSerialId <> '00000000-0000-0000-0000-000000000000'
    GROUP BY EquipmentSerialId
    HAVING COUNT(*) > 1
    ORDER BY COUNT(*) DESC, EquipmentSerialId
)
SELECT d.EquipmentSerialId,
       d.InstanceCount,
       i.Id AS ItemInstanceId,
       i.ItemVNum,
       i.Amount,
       i.CharacterId,
       CAST(i.Type AS int) AS InventoryTypeValue,
       i.Slot,
       i.Rare,
       i.Upgrade
FROM DuplicateSerials d
INNER JOIN dbo.ItemInstance i ON i.EquipmentSerialId = d.EquipmentSerialId
ORDER BY d.InstanceCount DESC, d.EquipmentSerialId, i.CharacterId, i.Type, i.Slot, i.Id;";

            return Query<DuplicateEquipmentSerialItemDTO>(sql,
                new SqlParameter("@TakeGroups", ClampTake(takeGroups)));
        }

        private static ItemTraceDTO LoadSingle(FrostveinContext context, Guid operationId, int sequence)
        {
            const string sql = @"SELECT TOP (1) * FROM dbo.ItemTrace
WHERE OperationId = @OperationId AND Sequence = @Sequence;";
            return context.Database.SqlQuery<ItemTraceDTO>(sql,
                new SqlParameter("@OperationId", operationId),
                new SqlParameter("@Sequence", sequence)).FirstOrDefault();
        }

        private static IEnumerable<T> Query<T>(string sql, params object[] parameters) where T : class
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<T>(sql, parameters).ToList();
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"Unable to query item integrity data for {typeof(T).Name}.", exception);
                return Enumerable.Empty<T>();
            }
        }

        private static object[] Parameters(ItemTraceDTO trace) => new object[]
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

    public sealed class GmCommandAuditDAO : IGmCommandAuditDAO
    {
        private const int MaximumTake = 100;
        private static int _failureLogged;

        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<int>(
                        "SELECT CASE WHEN OBJECT_ID(N'dbo.GmCommandAudit', N'U') IS NULL THEN 0 ELSE 1 END;")
                        .FirstOrDefault() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public GmCommandAuditDTO Insert(GmCommandAuditDTO audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (audit.CorrelationId == Guid.Empty) audit.CorrelationId = Guid.NewGuid();
            if (audit.OccurredAtUtc == default(DateTime)) audit.OccurredAtUtc = DateTime.UtcNow;

            audit.CharacterName = Limit(audit.CharacterName, 64);
            audit.CommandHeader = Limit(audit.CommandHeader, 64) ?? "<unknown>";
            audit.CommandText = Limit(audit.CommandText, 1000);
            audit.IpAddress = Limit(audit.IpAddress, 64);
            audit.Failure = Limit(audit.Failure, 2000);

            const string sql = @"
INSERT INTO dbo.GmCommandAudit
(CorrelationId, OccurredAtUtc, AccountId, CharacterId, CharacterName,
 Authority, CommandHeader, CommandText, RequiredAuthority, Outcome,
 IpAddress, ChannelId, MapId, SessionId, Failure)
VALUES
(@CorrelationId, @OccurredAtUtc, @AccountId, @CharacterId, @CharacterName,
 @Authority, @CommandHeader, @CommandText, @RequiredAuthority, @Outcome,
 @IpAddress, @ChannelId, @MapId, @SessionId, @Failure);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    audit.AuditId = context.Database.SqlQuery<long>(sql, Parameters(audit)).Single();
                    return audit;
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to append GmCommandAudit event. Apply the GM audit migration.", exception);
                return null;
            }
        }

        public IEnumerable<GmCommandAuditDTO> LoadRecent(int take = 30) =>
            Query(@"SELECT TOP (@Take) * FROM dbo.GmCommandAudit
ORDER BY OccurredAtUtc DESC, AuditId DESC;", new SqlParameter("@Take", ClampTake(take)));

        public IEnumerable<GmCommandAuditDTO> LoadByAccountId(long accountId, int take = 30) =>
            Query(@"SELECT TOP (@Take) * FROM dbo.GmCommandAudit
WHERE AccountId = @Value ORDER BY OccurredAtUtc DESC, AuditId DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", accountId));

        public IEnumerable<GmCommandAuditDTO> LoadByCharacterId(long characterId, int take = 30) =>
            Query(@"SELECT TOP (@Take) * FROM dbo.GmCommandAudit
WHERE CharacterId = @Value ORDER BY OccurredAtUtc DESC, AuditId DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", characterId));

        public IEnumerable<GmCommandAuditDTO> LoadByCommand(string commandHeader, int take = 30)
        {
            string normalized = NormalizeHeader(commandHeader);
            if (normalized == null) return Enumerable.Empty<GmCommandAuditDTO>();
            return Query(@"SELECT TOP (@Take) * FROM dbo.GmCommandAudit
WHERE CommandHeader = @Value ORDER BY OccurredAtUtc DESC, AuditId DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", normalized));
        }

        public IEnumerable<GmCommandAuditDTO> LoadByOutcome(GmCommandAuditOutcome outcome, int take = 30) =>
            Query(@"SELECT TOP (@Take) * FROM dbo.GmCommandAudit
WHERE Outcome = @Value ORDER BY OccurredAtUtc DESC, AuditId DESC;",
                new SqlParameter("@Take", ClampTake(take)), new SqlParameter("@Value", (byte)outcome));

        private static IEnumerable<GmCommandAuditDTO> Query(string sql, params object[] parameters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<GmCommandAuditDTO>(sql, parameters).ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to query GmCommandAudit. Apply the GM audit migration.", exception);
                return Enumerable.Empty<GmCommandAuditDTO>();
            }
        }

        private static object[] Parameters(GmCommandAuditDTO audit) => new object[]
        {
            Parameter("@CorrelationId", audit.CorrelationId), Parameter("@OccurredAtUtc", audit.OccurredAtUtc),
            Parameter("@AccountId", audit.AccountId), Parameter("@CharacterId", audit.CharacterId),
            Parameter("@CharacterName", audit.CharacterName), Parameter("@Authority", (short)audit.Authority),
            Parameter("@CommandHeader", audit.CommandHeader), Parameter("@CommandText", audit.CommandText),
            Parameter("@RequiredAuthority", (short)audit.RequiredAuthority), Parameter("@Outcome", (byte)audit.Outcome),
            Parameter("@IpAddress", audit.IpAddress), Parameter("@ChannelId", audit.ChannelId),
            Parameter("@MapId", audit.MapId), Parameter("@SessionId", audit.SessionId),
            Parameter("@Failure", audit.Failure)
        };

        private static SqlParameter Parameter(string name, object value) =>
            new SqlParameter(name, value ?? DBNull.Value);

        private static int ClampTake(int take) => take < 1 ? 1 : take > MaximumTake ? MaximumTake : take;

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string normalized = value.Trim();
            if (!normalized.StartsWith("$", StringComparison.Ordinal)) normalized = "$" + normalized;
            return Limit(normalized, 64);
        }

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }

        private static void LogFailureOnce(string message, Exception exception)
        {
            if (Interlocked.Exchange(ref _failureLogged, 1) == 0)
            {
                Logger.Error(message, exception);
            }
        }
    }

    public sealed class StaffPermissionDAO : IStaffPermissionDAO
    {
        private static int _failureLogged;

        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<int>(
                        "SELECT CASE WHEN OBJECT_ID(N'dbo.StaffPermissionProfile', N'U') IS NULL THEN 0 ELSE 1 END;")
                        .FirstOrDefault() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public StaffPermissionProfileDTO LoadByAccountId(long accountId)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<StaffPermissionProfileDTO>(@"
SELECT TOP (1) AccountId, PermissionMask, IsEnabled, UpdatedAtUtc,
       UpdatedByAccountId, UpdatedByCharacterId, Reason
FROM dbo.StaffPermissionProfile
WHERE AccountId = @AccountId;", new SqlParameter("@AccountId", accountId)).FirstOrDefault();
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                return null;
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to load StaffPermissionProfile.", exception);
                return null;
            }
        }

        public StaffPermissionProfileDTO Save(
            long accountId,
            long permissionMask,
            bool isEnabled,
            long? updatedByAccountId,
            long? updatedByCharacterId,
            string reason)
        {
            reason = Limit(reason, 500);
            const string sql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE dbo.StaffPermissionProfile WITH (UPDLOCK, HOLDLOCK)
SET PermissionMask = @PermissionMask,
    IsEnabled = @IsEnabled,
    UpdatedAtUtc = SYSUTCDATETIME(),
    UpdatedByAccountId = @UpdatedByAccountId,
    UpdatedByCharacterId = @UpdatedByCharacterId,
    Reason = @Reason
WHERE AccountId = @AccountId;
IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO dbo.StaffPermissionProfile
    (AccountId, PermissionMask, IsEnabled, UpdatedAtUtc,
     UpdatedByAccountId, UpdatedByCharacterId, Reason)
    VALUES
    (@AccountId, @PermissionMask, @IsEnabled, SYSUTCDATETIME(),
     @UpdatedByAccountId, @UpdatedByCharacterId, @Reason);
END
COMMIT TRANSACTION;
SELECT TOP (1) AccountId, PermissionMask, IsEnabled, UpdatedAtUtc,
       UpdatedByAccountId, UpdatedByCharacterId, Reason
FROM dbo.StaffPermissionProfile
WHERE AccountId = @AccountId;";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<StaffPermissionProfileDTO>(sql,
                        new SqlParameter("@AccountId", accountId),
                        new SqlParameter("@PermissionMask", permissionMask),
                        new SqlParameter("@IsEnabled", isEnabled),
                        Parameter("@UpdatedByAccountId", updatedByAccountId),
                        Parameter("@UpdatedByCharacterId", updatedByCharacterId),
                        Parameter("@Reason", reason)).SingleOrDefault();
                }
            }
            catch (SqlException exception) when (exception.Number == 208)
            {
                return null;
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to save StaffPermissionProfile.", exception);
                return null;
            }
        }

        private static SqlParameter Parameter(string name, object value) =>
            new SqlParameter(name, value ?? DBNull.Value);

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
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
