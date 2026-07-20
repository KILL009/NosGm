namespace Frostvein.Domain
{
    public enum GmSanctionActionType : byte
    {
        Warning = 1,
        Mute = 2,
        Ban = 3,
        IpBan = 4,
        Unmute = 5,
        Unban = 6
    }

    public static class GmSanctionActionTypeExtensions
    {
        public static bool IsReversal(this GmSanctionActionType action) =>
            action == GmSanctionActionType.Unmute || action == GmSanctionActionType.Unban;

        public static PenaltyType ToPenaltyType(this GmSanctionActionType action)
        {
            switch (action)
            {
                case GmSanctionActionType.Warning:
                    return PenaltyType.Warning;
                case GmSanctionActionType.Mute:
                case GmSanctionActionType.Unmute:
                    return PenaltyType.Muted;
                case GmSanctionActionType.Ban:
                case GmSanctionActionType.Unban:
                    return PenaltyType.Banned;
                case GmSanctionActionType.IpBan:
                    return PenaltyType.IPBanned;
                default:
                    return PenaltyType.Warning;
            }
        }
    }
}
