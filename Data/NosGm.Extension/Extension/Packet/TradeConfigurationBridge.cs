namespace NosGm.Extension.Extension.Packet
{
    /// <summary>
    /// Keeps the legacy misspelled configuration type available to the extension
    /// assembly without duplicating the actual limits.
    /// </summary>
    internal static class InventoryConfigrationExtension
    {
        public static readonly short MaxItemPerSlot =
            NosGm.GameObject.Extension.InventoryConfigrationExtension.MaxItemPerSlot;

        public static readonly int MaxGoldBank =
            NosGm.GameObject.Extension.InventoryConfigrationExtension.MaxGoldBank;
    }
}
