using Frostvein.Core;
using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace Frostvein.DAL.DAO
{
    public sealed class GmCaseDAO : IGmCaseDAO
    {
        private const int MaximumTake = 100;
        private static int _failureLogged;

        private const string CaseProjection = @"
SELECT c.CaseId, c.CorrelationId, c.CreatedAtUtc, c.UpdatedAtUtc, c.ClosedAtUtc,
       c.Status, c.Priority, c.SubjectType, c.SubjectAccountId, c.SubjectCharacterId,
       c.SubjectName, c.Title, c.Summary, c.CreatedByAccountId, c.CreatedByCharacterId,
       c.CreatedByName, c.AssignedAccountId, c.AssignedCharacterId, c.AssignedName,
       (SELECT COUNT(*) FROM dbo.GmCaseNote n WHERE n.CaseId = c.CaseId) AS NoteCount
FROM dbo.GmCase c ";

        public bool IsAvailable()
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<int>(@"
SELECT CASE WHEN OBJECT_ID(N'dbo.GmCase', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.GmCaseNote', N'U') IS NOT NULL
            THEN 1 ELSE 0 END;").FirstOrDefault() == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public GmCaseDTO Create(GmCaseDTO caseFile, GmCaseNoteDTO initialNote)
        {
            if (caseFile == null) throw new ArgumentNullException(nameof(caseFile));
            if (initialNote == null) throw new ArgumentNullException(nameof(initialNote));

            if (caseFile.CorrelationId == Guid.Empty) caseFile.CorrelationId = Guid.NewGuid();
            if (caseFile.CreatedAtUtc == default(DateTime)) caseFile.CreatedAtUtc = DateTime.UtcNow;
            caseFile.UpdatedAtUtc = caseFile.CreatedAtUtc;

            const string sql = @"
INSERT INTO dbo.GmCase
(CorrelationId, CreatedAtUtc, UpdatedAtUtc, ClosedAtUtc, Status, Priority,
 SubjectType, SubjectAccountId, SubjectCharacterId, SubjectName, Title, Summary,
 CreatedByAccountId, CreatedByCharacterId, CreatedByName,
 AssignedAccountId, AssignedCharacterId, AssignedName)
VALUES
(@CorrelationId, @CreatedAtUtc, @UpdatedAtUtc, NULL, @Status, @Priority,
 @SubjectType, @SubjectAccountId, @SubjectCharacterId, @SubjectName, @Title, @Summary,
 @CreatedByAccountId, @CreatedByCharacterId, @CreatedByName,
 @AssignedAccountId, @AssignedCharacterId, @AssignedName);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    caseFile.CaseId = context.Database.SqlQuery<long>(sql, CaseParameters(caseFile)).Single();
                    initialNote.CaseId = caseFile.CaseId;
                    if (initialNote.OccurredAtUtc == default(DateTime)) initialNote.OccurredAtUtc = caseFile.CreatedAtUtc;
                    initialNote.NoteId = InsertNote(context, initialNote);
                    transaction.Commit();
                }

                return LoadById(caseFile.CaseId);
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to create a GM case. Apply the GM case migration.", exception);
                return null;
            }
        }

        public GmCaseDTO LoadById(long caseId)
        {
            if (caseId <= 0) return null;
            return QueryCases(CaseProjection + " WHERE c.CaseId = @CaseId;",
                new SqlParameter("@CaseId", caseId)).FirstOrDefault();
        }

        public IEnumerable<GmCaseDTO> LoadRecent(int take = 20) =>
            QueryCases(CaseProjection + @"
ORDER BY c.UpdatedAtUtc DESC, c.CaseId DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY;", new SqlParameter("@Take", ClampTake(take)));

        public IEnumerable<GmCaseDTO> LoadByAssignedAccount(long accountId, int take = 20) =>
            QueryCases(CaseProjection + @"
WHERE c.AssignedAccountId = @AccountId AND c.Status IN (1, 2, 3)
ORDER BY c.Priority DESC, c.UpdatedAtUtc DESC, c.CaseId DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY;",
                new SqlParameter("@AccountId", accountId),
                new SqlParameter("@Take", ClampTake(take)));

        public IEnumerable<GmCaseDTO> LoadBySubject(long accountId, long? characterId, int take = 20) =>
            QueryCases(CaseProjection + @"
WHERE c.SubjectAccountId = @AccountId
  AND (@CharacterId IS NULL OR c.SubjectCharacterId = @CharacterId OR c.SubjectCharacterId IS NULL)
ORDER BY c.UpdatedAtUtc DESC, c.CaseId DESC
OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY;",
                new SqlParameter("@AccountId", accountId),
                Parameter("@CharacterId", characterId),
                new SqlParameter("@Take", ClampTake(take)));

        public IEnumerable<GmCaseNoteDTO> LoadNotes(long caseId, int take = 30)
        {
            if (caseId <= 0) return Enumerable.Empty<GmCaseNoteDTO>();
            const string sql = @"
SELECT TOP (@Take) NoteId, CaseId, OccurredAtUtc, NoteType, AuthorAccountId,
       AuthorCharacterId, AuthorName, Text, Reference, Metadata
FROM dbo.GmCaseNote
WHERE CaseId = @CaseId
ORDER BY OccurredAtUtc DESC, NoteId DESC;";
            return QueryNotes(sql,
                new SqlParameter("@Take", ClampTake(take)),
                new SqlParameter("@CaseId", caseId));
        }

        public GmCaseNoteDTO AddNote(GmCaseNoteDTO note)
        {
            if (note == null) throw new ArgumentNullException(nameof(note));
            if (note.CaseId <= 0) throw new ArgumentOutOfRangeException(nameof(note.CaseId));
            if (note.OccurredAtUtc == default(DateTime)) note.OccurredAtUtc = DateTime.UtcNow;

            try
            {
                using (var context = DataAccessHelper.CreateContext())
                using (var transaction = context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    int updated = context.Database.ExecuteSqlCommand(
                        "UPDATE dbo.GmCase SET UpdatedAtUtc = @OccurredAtUtc WHERE CaseId = @CaseId;",
                        new SqlParameter("@OccurredAtUtc", note.OccurredAtUtc),
                        new SqlParameter("@CaseId", note.CaseId));
                    if (updated == 0) return null;

                    note.NoteId = InsertNote(context, note);
                    transaction.Commit();
                    return note;
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to append a GM case note.", exception);
                return null;
            }
        }

        public GmCaseDTO Assign(
            long caseId,
            long? assignedAccountId,
            long? assignedCharacterId,
            string assignedName,
            GmCaseNoteDTO auditNote)
        {
            if (auditNote == null) throw new ArgumentNullException(nameof(auditNote));
            if (auditNote.OccurredAtUtc == default(DateTime)) auditNote.OccurredAtUtc = DateTime.UtcNow;

            const string sql = @"
UPDATE dbo.GmCase
SET AssignedAccountId = @AssignedAccountId,
    AssignedCharacterId = @AssignedCharacterId,
    AssignedName = @AssignedName,
    UpdatedAtUtc = @OccurredAtUtc
WHERE CaseId = @CaseId;";

            return MutateCase(caseId, sql, auditNote,
                Parameter("@AssignedAccountId", assignedAccountId),
                Parameter("@AssignedCharacterId", assignedCharacterId),
                Parameter("@AssignedName", Limit(assignedName, 64)));
        }

        public GmCaseDTO UpdateStatus(long caseId, GmCaseStatus status, GmCaseNoteDTO auditNote)
        {
            if (auditNote == null) throw new ArgumentNullException(nameof(auditNote));
            if (auditNote.OccurredAtUtc == default(DateTime)) auditNote.OccurredAtUtc = DateTime.UtcNow;

            const string sql = @"
UPDATE dbo.GmCase
SET Status = @Status,
    UpdatedAtUtc = @OccurredAtUtc,
    ClosedAtUtc = CASE WHEN @Status IN (4, 5) THEN @OccurredAtUtc ELSE NULL END
WHERE CaseId = @CaseId;";

            return MutateCase(caseId, sql, auditNote,
                new SqlParameter("@Status", (byte)status));
        }

        public GmCaseDTO UpdatePriority(long caseId, GmCasePriority priority, GmCaseNoteDTO auditNote)
        {
            if (auditNote == null) throw new ArgumentNullException(nameof(auditNote));
            if (auditNote.OccurredAtUtc == default(DateTime)) auditNote.OccurredAtUtc = DateTime.UtcNow;

            const string sql = @"
UPDATE dbo.GmCase
SET Priority = @Priority, UpdatedAtUtc = @OccurredAtUtc
WHERE CaseId = @CaseId;";

            return MutateCase(caseId, sql, auditNote,
                new SqlParameter("@Priority", (byte)priority));
        }

        private GmCaseDTO MutateCase(
            long caseId,
            string updateSql,
            GmCaseNoteDTO auditNote,
            params object[] extraParameters)
        {
            if (caseId <= 0) return null;
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                using (var transaction = context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    var parameters = new List<object>(extraParameters ?? Array.Empty<object>())
                    {
                        new SqlParameter("@OccurredAtUtc", auditNote.OccurredAtUtc),
                        new SqlParameter("@CaseId", caseId)
                    };
                    int updated = context.Database.ExecuteSqlCommand(updateSql, parameters.ToArray());
                    if (updated == 0) return null;

                    auditNote.CaseId = caseId;
                    auditNote.NoteId = InsertNote(context, auditNote);
                    transaction.Commit();
                }
                return LoadById(caseId);
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to update a GM case.", exception);
                return null;
            }
        }

        private static long InsertNote(FrostveinContext context, GmCaseNoteDTO note)
        {
            const string sql = @"
INSERT INTO dbo.GmCaseNote
(CaseId, OccurredAtUtc, NoteType, AuthorAccountId, AuthorCharacterId,
 AuthorName, Text, Reference, Metadata)
VALUES
(@CaseId, @OccurredAtUtc, @NoteType, @AuthorAccountId, @AuthorCharacterId,
 @AuthorName, @Text, @Reference, @Metadata);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            return context.Database.SqlQuery<long>(sql,
                new SqlParameter("@CaseId", note.CaseId),
                new SqlParameter("@OccurredAtUtc", note.OccurredAtUtc),
                new SqlParameter("@NoteType", (byte)note.NoteType),
                new SqlParameter("@AuthorAccountId", note.AuthorAccountId),
                Parameter("@AuthorCharacterId", note.AuthorCharacterId),
                Parameter("@AuthorName", Limit(note.AuthorName, 64)),
                new SqlParameter("@Text", Limit(note.Text, 2000) ?? string.Empty),
                Parameter("@Reference", Limit(note.Reference, 500)),
                Parameter("@Metadata", Limit(note.Metadata, 2000))).Single();
        }

        private static IEnumerable<GmCaseDTO> QueryCases(string sql, params object[] parameters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<GmCaseDTO>(sql, parameters).ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to query GM cases. Apply the GM case migration.", exception);
                return Enumerable.Empty<GmCaseDTO>();
            }
        }

        private static IEnumerable<GmCaseNoteDTO> QueryNotes(string sql, params object[] parameters)
        {
            try
            {
                using (var context = DataAccessHelper.CreateContext())
                {
                    return context.Database.SqlQuery<GmCaseNoteDTO>(sql, parameters).ToList();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce("Unable to query GM case notes.", exception);
                return Enumerable.Empty<GmCaseNoteDTO>();
            }
        }

        private static object[] CaseParameters(GmCaseDTO value) => new object[]
        {
            new SqlParameter("@CorrelationId", value.CorrelationId),
            new SqlParameter("@CreatedAtUtc", value.CreatedAtUtc),
            new SqlParameter("@UpdatedAtUtc", value.UpdatedAtUtc),
            new SqlParameter("@Status", (byte)value.Status),
            new SqlParameter("@Priority", (byte)value.Priority),
            new SqlParameter("@SubjectType", (byte)value.SubjectType),
            new SqlParameter("@SubjectAccountId", value.SubjectAccountId),
            Parameter("@SubjectCharacterId", value.SubjectCharacterId),
            Parameter("@SubjectName", Limit(value.SubjectName, 64)),
            new SqlParameter("@Title", Limit(value.Title, 160) ?? "Untitled case"),
            Parameter("@Summary", Limit(value.Summary, 2000)),
            new SqlParameter("@CreatedByAccountId", value.CreatedByAccountId),
            Parameter("@CreatedByCharacterId", value.CreatedByCharacterId),
            Parameter("@CreatedByName", Limit(value.CreatedByName, 64)),
            Parameter("@AssignedAccountId", value.AssignedAccountId),
            Parameter("@AssignedCharacterId", value.AssignedCharacterId),
            Parameter("@AssignedName", Limit(value.AssignedName, 64))
        };

        private static SqlParameter Parameter(string name, object value) =>
            new SqlParameter(name, value ?? DBNull.Value);

        private static int ClampTake(int take) => take < 1 ? 1 : take > MaximumTake ? MaximumTake : take;

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