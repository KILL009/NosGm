namespace NosGm.Domain
{
    public enum BazaarAuditEventType : byte
    {
        Listing = 1,
        Purchase = 2,
        PriceChange = 3,
        Recollect = 4
    }

    public enum BazaarAuditSeverity : byte
    {
        Information = 1,
        Warning = 2,
        Critical = 3
    }
}
