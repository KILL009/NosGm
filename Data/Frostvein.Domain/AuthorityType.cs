namespace Frostvein.Domain
{
    public enum AuthorityType : short
    {
        Closed = -3,
        Banned = -2,
        Unconfirmed = -1,
        User = 0,
        GS = 1,
        GM = 2,
        ADMIN = 3,
        DEV = 4
    }

    /// <summary>
    /// Result captured after a staff command handler returns or throws.
    /// Denied is reserved for the authorization gate that will be wired in the
    /// granular staff-permission phase.
    /// </summary>
    public enum GmCommandAuditOutcome : byte
    {
        Executed = 1,
        Failed = 2,
        Denied = 3
    }
}
