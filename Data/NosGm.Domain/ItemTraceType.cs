namespace NosGm.Domain
{
    /// <summary>
    /// Describes what happened to a persistent item instance.
    /// Values are stored in the append-only ItemTrace table, so never reorder them.
    /// </summary>
    public enum ItemTraceAction
    {
        Unknown = 0,
        Created = 1,
        Updated = 2,
        Transferred = 3,
        StackChanged = 4,
        Consumed = 5,
        Deleted = 6,
        Quarantined = 7,
        Released = 8,
        Restored = 9
    }

    /// <summary>
    /// Identifies the subsystem that caused an item mutation.
    /// Values are persisted and must remain stable.
    /// </summary>
    public enum ItemTraceSource
    {
        Unknown = 0,
        Persistence = 1,
        Drop = 2,
        Reward = 3,
        Raid = 4,
        Quest = 5,
        Crafting = 6,
        Upgrade = 7,
        Trade = 8,
        Bazaar = 9,
        Mail = 10,
        Shop = 11,
        ItemMall = 12,
        GameMaster = 13,
        System = 14,
        Migration = 15
    }
}
