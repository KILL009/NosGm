using Frostvein.DAL.DAO;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;

namespace Frostvein.DAL
{
    public sealed class GmCaseService
    {
        private static readonly Lazy<GmCaseService> LazyInstance =
            new Lazy<GmCaseService>(() => new GmCaseService(new GmCaseDAO()));

        private readonly IGmCaseDAO _caseDao;

        internal GmCaseService(IGmCaseDAO caseDao)
        {
            _caseDao = caseDao ?? throw new ArgumentNullException(nameof(caseDao));
        }

        public static GmCaseService Instance => LazyInstance.Value;

        public bool IsAvailable() => _caseDao.IsAvailable();

        public GmCaseDTO Create(
            GmCaseSubjectType subjectType,
            long subjectAccountId,
            long? subjectCharacterId,
            string subjectName,
            GmCasePriority priority,
            string reason,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            if (subjectAccountId <= 0) throw new ArgumentOutOfRangeException(nameof(subjectAccountId));
            string normalizedReason = Limit(reason, 2000);
            if (string.IsNullOrWhiteSpace(normalizedReason))
                throw new ArgumentException("A case requires a reason.", nameof(reason));

            DateTime now = DateTime.UtcNow;
            var caseFile = new GmCaseDTO
            {
                CorrelationId = Guid.NewGuid(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Status = GmCaseStatus.Open,
                Priority = priority,
                SubjectType = subjectType,
                SubjectAccountId = subjectAccountId,
                SubjectCharacterId = subjectCharacterId,
                SubjectName = Limit(subjectName, 64),
                Title = Limit(normalizedReason, 160),
                Summary = normalizedReason,
                CreatedByAccountId = actorAccountId,
                CreatedByCharacterId = actorCharacterId,
                CreatedByName = Limit(actorName, 64),
                AssignedAccountId = actorAccountId,
                AssignedCharacterId = actorCharacterId,
                AssignedName = Limit(actorName, 64)
            };

            return _caseDao.Create(caseFile, NewNote(
                GmCaseNoteType.Opened,
                normalizedReason,
                null,
                actorAccountId,
                actorCharacterId,
                actorName,
                now));
        }

        public GmCaseDTO Get(long caseId) => _caseDao.LoadById(caseId);

        public IEnumerable<GmCaseDTO> GetRecent(int take = 20) => _caseDao.LoadRecent(take);

        public IEnumerable<GmCaseDTO> GetMine(long accountId, int take = 20) =>
            _caseDao.LoadByAssignedAccount(accountId, take);

        public IEnumerable<GmCaseDTO> GetBySubject(long accountId, long? characterId, int take = 20) =>
            _caseDao.LoadBySubject(accountId, characterId, take);

        public IEnumerable<GmCaseNoteDTO> GetNotes(long caseId, int take = 30) =>
            _caseDao.LoadNotes(caseId, take);

        public GmCaseNoteDTO AddNote(
            long caseId,
            string text,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            string normalized = Limit(text, 2000);
            if (caseId <= 0 || string.IsNullOrWhiteSpace(normalized)) return null;
            return _caseDao.AddNote(NewNote(
                GmCaseNoteType.Note,
                normalized,
                null,
                actorAccountId,
                actorCharacterId,
                actorName,
                DateTime.UtcNow,
                caseId));
        }

        public GmCaseNoteDTO AddEvidence(
            long caseId,
            string reference,
            string description,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            string normalizedReference = Limit(reference, 500);
            if (caseId <= 0 || string.IsNullOrWhiteSpace(normalizedReference)) return null;
            string text = Limit(description, 2000) ?? "Evidence attached.";
            return _caseDao.AddNote(NewNote(
                GmCaseNoteType.Evidence,
                text,
                normalizedReference,
                actorAccountId,
                actorCharacterId,
                actorName,
                DateTime.UtcNow,
                caseId));
        }

        public GmCaseDTO Assign(
            long caseId,
            long? assignedAccountId,
            long? assignedCharacterId,
            string assignedName,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            string target = assignedAccountId.HasValue
                ? $"Assigned to {Limit(assignedName, 64) ?? assignedAccountId.Value.ToString()}."
                : "Assignment cleared.";
            return _caseDao.Assign(
                caseId,
                assignedAccountId,
                assignedCharacterId,
                Limit(assignedName, 64),
                NewNote(GmCaseNoteType.Assignment, target, null,
                    actorAccountId, actorCharacterId, actorName, DateTime.UtcNow, caseId));
        }

        public GmCaseDTO UpdateStatus(
            long caseId,
            GmCaseStatus status,
            string reason,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            string text = $"Status changed to {status}.";
            string normalizedReason = Limit(reason, 1800);
            if (!string.IsNullOrWhiteSpace(normalizedReason)) text += " " + normalizedReason;
            return _caseDao.UpdateStatus(
                caseId,
                status,
                NewNote(GmCaseNoteType.StatusChange, text, null,
                    actorAccountId, actorCharacterId, actorName, DateTime.UtcNow, caseId));
        }

        public GmCaseDTO UpdatePriority(
            long caseId,
            GmCasePriority priority,
            string reason,
            long actorAccountId,
            long? actorCharacterId,
            string actorName)
        {
            string text = $"Priority changed to {priority}.";
            string normalizedReason = Limit(reason, 1800);
            if (!string.IsNullOrWhiteSpace(normalizedReason)) text += " " + normalizedReason;
            return _caseDao.UpdatePriority(
                caseId,
                priority,
                NewNote(GmCaseNoteType.PriorityChange, text, null,
                    actorAccountId, actorCharacterId, actorName, DateTime.UtcNow, caseId));
        }

        private static GmCaseNoteDTO NewNote(
            GmCaseNoteType type,
            string text,
            string reference,
            long actorAccountId,
            long? actorCharacterId,
            string actorName,
            DateTime occurredAtUtc,
            long caseId = 0) => new GmCaseNoteDTO
        {
            CaseId = caseId,
            OccurredAtUtc = occurredAtUtc,
            NoteType = type,
            AuthorAccountId = actorAccountId,
            AuthorCharacterId = actorCharacterId,
            AuthorName = Limit(actorName, 64),
            Text = Limit(text, 2000),
            Reference = Limit(reference, 500)
        };

        private static string Limit(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length <= maximumLength ? trimmed : trimmed.Substring(0, maximumLength);
        }
    }
}