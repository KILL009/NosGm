using NosGm.Domain;

public class InventoryItem
{
    public short VNum { get; set; }
    public short Amount { get; set; }
    public InventoryType InventoryType { get; set; }
    public sbyte Rare { get; set; }
    public byte Design { get; set; }
}