using System;

namespace Frostvein.Domain
{
    public enum GmCaseStatus : byte
    {
        Open = 1,
        Investigating = 2,
        Waiting = 3,
        Resolved = 4,
        Dismissed = 5
    }

    public enum GmCasePriority : byte
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    public enum GmCaseSubjectType : byte
    {
        Account = 1,
        Character = 2
    }

    public enum GmCaseNoteType : byte
    {
        Note = 1,
        Evidence = 2,
        Assignment = 3,
        StatusChange = 4,
        PriorityChange = 5,
        Opened = 6,
        SanctionApplied = 7,
        SanctionReversed = 8
    }
}
