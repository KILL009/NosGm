using NosGm.Core;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace NosGm.DAL.DAO
{
    public sealed class GmSanctionDAO : IGmSanctionDAO
    {
        private const int MaximumTake = 100;
        private static int _failureLogged;

        private const string ActionProjection = @"
SELECT ActionId, OperationId, CaseId, OccurredAtUtc, ActionType, PenaltyLogId,
       AffectedPenaltyCount, SubjectAccountId, SubjectCharacterId, SubjectName,
       DurationValue, PenaltyEnd, Reason, ActorAccountId, ActorCharacterId, ActorName
FROM dbo.GmSanctionAction ";

        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<int>(@"
SELECT CASE WHEN OBJECT_ID(N'dbo.GmSanctionAction', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.GmCase', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.GmCaseNote', N'U') IS NOT NULL
            THEN 1 ELSE 0 END;").FirstOrDefault() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public GmSanctionResultDTO Execute(GmSanctionRequestDTO request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.OperationId == Guid.Empty) request.OperationId = Guid.NewGuid();
            if (request.OccurredAtUtc == default(DateTime)) request.OccurredAtUtc = DateTime.UtcNow;
            if (request.PenaltyStart == default(DateTime)) request.PenaltyStart = DateTime.Now;

            try
            {
                return request.ActionType.IsReversal()
                    ? Revoke(request)
                    : Apply(request);
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to persist a case-linked sanction. Apply the sanction migration.", exception);
                return Failed("Sanction persistence failed. Inspect the server log.");
            }
        }

        public IEnumerable<GmSanctionActionDTO> LoadByCase(long caseId, int take = 20)
        {
            if (caseId <= 0) return Enumerable.Empty<GmSanctionActionDTO>();

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<GmSanctionActionDTO>(ActionProjection + @"
WHERE CaseId = @CaseId
ORDER BY OccurredAtUtc DESC, ActionId DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY;",
                            new SqlParameter("@CaseId", caseId),
                            new SqlParameter("@Take", ClampTake(take)))
                        .ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to read GM sanction history.", exception);
                return Enumerable.Empty<GmSanctionActionDTO>();
            }
        }

        private static GmSanctionResultDTO Apply(GmSanctionRequestDTO request)
        {
            using (var context = DataAccessHelper.CreateContext())
            using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                GmSanctionActionDTO existing = LoadOperation(context, request.OperationId);
                if (existing != null)
                {
                    transaction.Commit();
                    return new GmSanctionResultDTO
                    {
                        Success = true,
                        AlreadyCompleted = true,
                        Action = existing
                    };
                }

                CaseSubjectRow caseFile = LoadAndLockCase(context, request.CaseId);
                string caseError = ValidateCase(caseFile, request);
                if (caseError != null) return Failed(caseError);

                PenaltyType penaltyType = request.ActionType.ToPenaltyType();
                if (request.ActionType != GmSanctionActionType.Warning &&
                    HasActivePenalty(context, request.SubjectAccountId, request.ActionType, request.PenaltyStart))
                {
                    return Failed("An active sanction of the same family already exists for this account.");
                }

                DateTime penaltyEnd = request.ActionType == GmSanctionActionType.Warning
                    ? request.PenaltyStart
                    : request.PenaltyEnd.GetValueOrDefault();
                if (request.ActionType != GmSanctionActionType.Warning && penaltyEnd <= request.PenaltyStart)
                    return Failed("The sanction end time must be after its start time.");

                const string insertPenaltySql = @"
INSERT INTO dbo.PenaltyLog
(AccountId, AdminName, DateEnd, DateStart, IP, Penalty, Reason)
VALUES
(@AccountId, @AdminName, @DateEnd, @DateStart, @IP, @Penalty, @Reason);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                string reason = Limit(request.Reason, 255);
                int penaltyLogId = context.Database.SqlQuery<int>(insertPenaltySql,
                    new SqlParameter("@AccountId", request.SubjectAccountId),
                    new SqlParameter("@AdminName", Limit(request.ActorName, 64) ?? "SYSTEM"),
                    new SqlParameter("@DateEnd", penaltyEnd),
                    new SqlParameter("@DateStart", request.PenaltyStart),
                    Parameter("@IP", Limit(request.IpAddress, 64)),
                    new SqlParameter("@Penalty", (byte)penaltyType),
                    new SqlParameter("@Reason", reason)).Single();

                var action = NewAction(request, penaltyLogId, 1, penaltyEnd);
                action.ActionId = InsertAction(context, action);
                AppendCaseNote(context, action, request, false);
                TouchCase(context, request.CaseId, request.OccurredAtUtc);
                transaction.Commit();

                return new GmSanctionResultDTO
                {
                    Success = true,
                    Action = action,
                    Penalty = new PenaltyLogDTO
                    {
                        PenaltyLogId = penaltyLogId,
                        AccountId = request.SubjectAccountId,
                        AdminName = Limit(request.ActorName, 64),
                        DateStart = request.PenaltyStart,
                        DateEnd = penaltyEnd,
                        IP = Limit(request.IpAddress, 64),
                        Penalty = penaltyType,
                        Reason = reason
                    }
                };
            }
        }

        private static GmSanctionResultDTO Revoke(GmSanctionRequestDTO request)
        {
            using (var context = DataAccessHelper.CreateContext())
            using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                GmSanctionActionDTO existing = LoadOperation(context, request.OperationId);
                if (existing != null)
                {
                    transaction.Commit();
                    return new GmSanctionResultDTO
                    {
                        Success = true,
                        AlreadyCompleted = true,
                        Action = existing
                    };
                }

                CaseSubjectRow caseFile = LoadAndLockCase(context, request.CaseId);
                string caseError = ValidateCase(caseFile, request, allowClosed: true);
                if (caseError != null) return Failed(caseError);

                string activePredicate = request.ActionType == GmSanctionActionType.Unmute
                    ? "Penalty = @Muted"
                    : "Penalty IN (@Banned, @IpBanned)";

                string selectSql = $@"
SELECT PenaltyLogId
FROM dbo.PenaltyLog WITH (UPDLOCK, HOLDLOCK)
WHERE AccountId = @AccountId
  AND DateEnd > @Now
  AND {activePredicate};";

                var activeParameters = new[]
                {
                    new SqlParameter("@AccountId", request.SubjectAccountId),
                    new SqlParameter("@Now", request.PenaltyStart),
                    new SqlParameter("@Muted", (byte)PenaltyType.Muted),
                    new SqlParameter("@Banned", (byte)PenaltyType.Banned),
                    new SqlParameter("@IpBanned", (byte)PenaltyType.IPBanned)
                };

                List<int> activeIds = context.Database.SqlQuery<int>(selectSql, activeParameters).ToList();
                if (activeIds.Count == 0) return Failed("No active sanction of that type exists for this account.");

                string updateSql = $@"
UPDATE dbo.PenaltyLog
SET DateEnd = @Now
WHERE AccountId = @AccountId
  AND DateEnd > @Now
  AND {activePredicate};";
                context.Database.ExecuteSqlCommand(updateSql,
                    new SqlParameter("@AccountId", request.SubjectAccountId),
                    new SqlParameter("@Now", request.PenaltyStart),
                    new SqlParameter("@Muted", (byte)PenaltyType.Muted),
                    new SqlParameter("@Banned", (byte)PenaltyType.Banned),
                    new SqlParameter("@IpBanned", (byte)PenaltyType.IPBanned));

                var action = NewAction(request, activeIds[0], activeIds.Count, request.PenaltyStart);
                action.ActionId = InsertAction(context, action);
                AppendCaseNote(context, action, request, true);
                TouchCase(context, request.CaseId, request.OccurredAtUtc);
                transaction.Commit();

                return new GmSanctionResultDTO
                {
                    Success = true,
                    Action = action
                };
            }
        }

        private static bool HasActivePenalty(
            System.Data.Entity.DbContext context,
            long accountId,
            GmSanctionActionType actionType,
            DateTime now)
        {
            string predicate = actionType == GmSanctionActionType.Mute
                ? "Penalty = @Muted"
                : "Penalty IN (@Banned, @IpBanned)";
            string sql = $@"
SELECT COUNT(1)
FROM dbo.PenaltyLog WITH (UPDLOCK, HOLDLOCK)
WHERE AccountId = @AccountId
  AND DateEnd > @Now
  AND {predicate};";

            return context.Database.SqlQuery<int>(sql,
                new SqlParameter("@AccountId", accountId),
                new SqlParameter("@Now", now),
                new SqlParameter("@Muted", (byte)PenaltyType.Muted),
                new SqlParameter("@Banned", (byte)PenaltyType.Banned),
                new SqlParameter("@IpBanned", (byte)PenaltyType.IPBanned)).Single() > 0;
        }

        private static CaseSubjectRow LoadAndLockCase(System.Data.Entity.DbContext context, long caseId) =>
            context.Database.SqlQuery<CaseSubjectRow>(@"
SELECT CaseId, SubjectAccountId, SubjectCharacterId, Status
FROM dbo.GmCase WITH (UPDLOCK, HOLDLOCK)
WHERE CaseId = @CaseId;", new SqlParameter("@CaseId", caseId)).FirstOrDefault();

        private static string ValidateCase(CaseSubjectRow caseFile, GmSanctionRequestDTO request, bool allowClosed = false)
        {
            if (caseFile == null) return "The GM case does not exist.";
            if (caseFile.SubjectAccountId != request.SubjectAccountId)
                return "The case subject does not match the target account.";
            if (caseFile.SubjectCharacterId.HasValue && request.SubjectCharacterId.HasValue &&
                caseFile.SubjectCharacterId.Value != request.SubjectCharacterId.Value)
                return "The case subject does not match the target character.";
            if (!allowClosed && caseFile.Status != (byte)GmCaseStatus.Open &&
                caseFile.Status != (byte)GmCaseStatus.Investigating &&
                caseFile.Status != (byte)GmCaseStatus.Waiting)
                return "The case must be open, investigating or waiting before applying a sanction.";
            if (caseFile.Status == (byte)GmCaseStatus.Dismissed)
                return "A dismissed case cannot be used for sanctions.";
            return null;
        }

        private static GmSanctionActionDTO LoadOperation(System.Data.Entity.DbContext context, Guid operationId) =>
            context.Database.SqlQuery<GmSanctionActionDTO>(ActionProjection + @"
WHERE OperationId = @OperationId;", new SqlParameter("@OperationId", operationId)).FirstOrDefault();

        private static long InsertAction(System.Data.Entity.DbContext context, GmSanctionActionDTO action)
        {
            const string sql = @"
INSERT INTO dbo.GmSanctionAction
(OperationId, CaseId, OccurredAtUtc, ActionType, PenaltyLogId, AffectedPenaltyCount,
 SubjectAccountId, SubjectCharacterId, SubjectName, DurationValue, PenaltyEnd, Reason,
 ActorAccountId, ActorCharacterId, ActorName)
VALUES
(@OperationId, @CaseId, @OccurredAtUtc, @ActionType, @PenaltyLogId, @AffectedPenaltyCount,
 @SubjectAccountId, @SubjectCharacterId, @SubjectName, @DurationValue, @PenaltyEnd, @Reason,
 @ActorAccountId, @ActorCharacterId, @ActorName);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            return context.Database.SqlQuery<long>(sql,
                new SqlParameter("@OperationId", action.OperationId),
                new SqlParameter("@CaseId", action.CaseId),
                new SqlParameter("@OccurredAtUtc", action.OccurredAtUtc),
                new SqlParameter("@ActionType", (byte)action.ActionType),
                Parameter("@PenaltyLogId", action.PenaltyLogId),
                new SqlParameter("@AffectedPenaltyCount", action.AffectedPenaltyCount),
                new SqlParameter("@SubjectAccountId", action.SubjectAccountId),
                Parameter("@SubjectCharacterId", action.SubjectCharacterId),
                Parameter("@SubjectName", Limit(action.SubjectName, 64)),
                new SqlParameter("@DurationValue", action.DurationValue),
                Parameter("@PenaltyEnd", action.PenaltyEnd),
                new SqlParameter("@Reason", Limit(action.Reason, 255)),
                new SqlParameter("@ActorAccountId", action.ActorAccountId),
                Parameter("@ActorCharacterId", action.ActorCharacterId),
                Parameter("@ActorName", Limit(action.ActorName, 64))).Single();
        }

        private static void AppendCaseNote(
            System.Data.Entity.DbContext context,
            GmSanctionActionDTO action,
            GmSanctionRequestDTO request,
            bool reversal)
        {
            string duration = request.DurationValue > 0 ? $" duration={request.DurationValue}" : string.Empty;
            string text = $"Sanction {(reversal ? "reversed" : "applied")}: {request.ActionType}.{duration} " +
                          $"Target={Limit(request.SubjectName, 64) ?? request.SubjectAccountId.ToString()}. " +
                          $"Reason={Limit(request.Reason, 255)}";
            string reference = $"sanction:{action.ActionId}";
            string metadata = $"operation={action.OperationId};penaltyLog={action.PenaltyLogId};affected={action.AffectedPenaltyCount}";

            context.Database.ExecuteSqlCommand(@"
INSERT INTO dbo.GmCaseNote
(CaseId, OccurredAtUtc, NoteType, AuthorAccountId, AuthorCharacterId,
 AuthorName, [Text], [Reference], Metadata)
VALUES
(@CaseId, @OccurredAtUtc, @NoteType, @AuthorAccountId, @AuthorCharacterId,
 @AuthorName, @Text, @Reference, @Metadata);",
                new SqlParameter("@CaseId", request.CaseId),
                new SqlParameter("@OccurredAtUtc", request.OccurredAtUtc),
                new SqlParameter("@NoteType", (byte)(reversal ? GmCaseNoteType.SanctionReversed : GmCaseNoteType.SanctionApplied)),
                new SqlParameter("@AuthorAccountId", request.ActorAccountId),
                Parameter("@AuthorCharacterId", request.ActorCharacterId),
                Parameter("@AuthorName", Limit(request.ActorName, 64)),
                new SqlParameter("@Text", Limit(text, 2000)),
                new SqlParameter("@Reference", reference),
                new SqlParameter("@Metadata", Limit(metadata, 2000)));
        }

        private static void TouchCase(System.Data.Entity.DbContext context, long caseId, DateTime occurredAtUtc) =>
            context.Database.ExecuteSqlCommand(@"
UPDATE dbo.GmCase
SET UpdatedAtUtc = @OccurredAtUtc,
    Status = CASE WHEN Status = 1 THEN 2 ELSE Status END
WHERE CaseId = @CaseId;",
                new SqlParameter("@OccurredAtUtc", occurredAtUtc),
                new SqlParameter("@CaseId", caseId));

        private static GmSanctionActionDTO NewAction(
            GmSanctionRequestDTO request,
            int? penaltyLogId,
            int affectedPenaltyCount,
            DateTime? penaltyEnd) => new GmSanctionActionDTO
        {
            OperationId = request.OperationId,
            CaseId = request.CaseId,
            OccurredAtUtc = request.OccurredAtUtc,
            ActionType = request.ActionType,
            PenaltyLogId = penaltyLogId,
            AffectedPenaltyCount = affectedPenaltyCount,
            SubjectAccountId = request.SubjectAccountId,
            SubjectCharacterId = request.SubjectCharacterId,
            SubjectName = Limit(request.SubjectName, 64),
            DurationValue = request.DurationValue,
            PenaltyEnd = penaltyEnd,
            Reason = Limit(request.Reason, 255),
            ActorAccountId = request.ActorAccountId,
            ActorCharacterId = request.ActorCharacterId,
            ActorName = Limit(request.ActorName, 64)
        };

        private static GmSanctionResultDTO Failed(string error) => new GmSanctionResultDTO
        {
            Success = false,
            Error = error
        };

        private static SqlParameter Parameter(string name, object value) =>
            new SqlParameter(name, value ?? DBNull.Value);

        private static int ClampTake(int take) => Math.Max(1, Math.Min(MaximumTake, take));

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }

        private static void LogFailureOnce(string message, Exception exception)
        {
            if (Interlocked.Exchange(ref _failureLogged, 1) == 0)
                Logger.Error(message, exception);
        }

        private sealed class CaseSubjectRow
        {
            public long CaseId { get; set; }
            public long SubjectAccountId { get; set; }
            public long? SubjectCharacterId { get; set; }
            public byte Status { get; set; }
        }
    }
}
