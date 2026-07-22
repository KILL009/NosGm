using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public sealed class GmSanctionRequestDTO
    {
        public Guid OperationId { get; set; }
        public long CaseId { get; set; }
        public GmSanctionActionType ActionType { get; set; }
        public long SubjectAccountId { get; set; }
        public long? SubjectCharacterId { get; set; }
        public string SubjectName { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public DateTime PenaltyStart { get; set; }
        public DateTime? PenaltyEnd { get; set; }
        public int DurationValue { get; set; }
        public string Reason { get; set; }
        public string IpAddress { get; set; }
        public long ActorAccountId { get; set; }
        public long? ActorCharacterId { get; set; }
        public string ActorName { get; set; }
    }

    [Serializable]
    public sealed class GmSanctionActionDTO
    {
        public long ActionId { get; set; }
        public Guid OperationId { get; set; }
        public long CaseId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public GmSanctionActionType ActionType { get; set; }
        public int? PenaltyLogId { get; set; }
        public int AffectedPenaltyCount { get; set; }
        public long SubjectAccountId { get; set; }
        public long? SubjectCharacterId { get; set; }
        public string SubjectName { get; set; }
        public int DurationValue { get; set; }
        public DateTime? PenaltyEnd { get; set; }
        public string Reason { get; set; }
        public long ActorAccountId { get; set; }
        public long? ActorCharacterId { get; set; }
        public string ActorName { get; set; }
    }

    [Serializable]
    public sealed class GmSanctionResultDTO
    {
        public bool Success { get; set; }
        public bool AlreadyCompleted { get; set; }
        public string Error { get; set; }
        public GmSanctionActionDTO Action { get; set; }
        public PenaltyLogDTO Penalty { get; set; }
    }
}
