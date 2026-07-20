using System;

namespace Frostvein.Data
{
    /// <summary>
    /// Read-only projection used by the item integrity tools. One row represents
    /// one live ItemInstance that shares an equipment serial with the group.
    /// </summary>
    [Serializable]
    public sealed class DuplicateEquipmentSerialItemDTO
    {
        public Guid EquipmentSerialId { get; set; }

        public int InstanceCount { get; set; }

        public Guid ItemInstanceId { get; set; }

        public short ItemVNum { get; set; }

        public int Amount { get; set; }

        public long CharacterId { get; set; }

        public int InventoryTypeValue { get; set; }

        public short Slot { get; set; }

        public short Rare { get; set; }

        public byte Upgrade { get; set; }
    }
}
