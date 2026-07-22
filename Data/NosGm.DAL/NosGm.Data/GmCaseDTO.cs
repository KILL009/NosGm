using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public sealed class GmCaseDTO
    {
        public long CaseId { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public GmCaseStatus Status { get; set; }

        public GmCasePriority Priority { get; set; }

        public GmCaseSubjectType SubjectType { get; set; }

        public long SubjectAccountId { get; set; }

        public long? SubjectCharacterId { get; set; }

        public string SubjectName { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public long CreatedByAccountId { get; set; }

        public long? CreatedByCharacterId { get; set; }

        public string CreatedByName { get; set; }

        public long? AssignedAccountId { get; set; }

        public long? AssignedCharacterId { get; set; }

        public string AssignedName { get; set; }

        public int NoteCount { get; set; }
    }

    [Serializable]
    public sealed class GmCaseNoteDTO
    {
        public long NoteId { get; set; }

        public long CaseId { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public GmCaseNoteType NoteType { get; set; }

        public long AuthorAccountId { get; set; }

        public long? AuthorCharacterId { get; set; }

        public string AuthorName { get; set; }

        public string Text { get; set; }

        public string Reference { get; set; }

        public string Metadata { get; set; }
    }
}