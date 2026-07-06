using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Frostvein.DAL.EF
{
    public class ItemInstance : SynchronizableBaseEntity
    {
        #region Instantiation

        public ItemInstance()
        {
            BazaarItem = new HashSet<BazaarItem>();
            MinilandObject = new HashSet<MinilandObject>();
        }

        #endregion

        #region Properties

        public byte? Ammo { get; set; }

        public int Amount { get; set; }

        public virtual ICollection<BazaarItem> BazaarItem { get; set; }

        public long? BazaarItemId { get; set; }

        [ForeignKey(nameof(BoundCharacterId))] public Character BoundCharacter { get; set; }

        public long? BoundCharacterId { get; set; }

        public byte? Cellon { get; set; }

        public virtual Character Character { get; set; }

        [Index("IX_SlotAndType", 1, IsUnique = false, Order = 0)]
        public long CharacterId { get; set; }

        public short? CloseDefence { get; set; }

        public short? Concentrate { get; set; }

        public short? CriticalDodge { get; set; }

        public byte? CriticalLuckRate { get; set; }

        public short? CriticalRate { get; set; }

        public short? DamageMaximum { get; set; }

        public short? DamageMinimum { get; set; }

        public byte? DarkElement { get; set; }

        public short? DarkResistance { get; set; }

        public short? DefenceDodge { get; set; }

        public short Design { get; set; }

        public short? DistanceDefence { get; set; }

        public short? DistanceDefenceDodge { get; set; }

        public int DurabilityPoint { get; set; }

        public short? ElementRate { get; set; }

        public Guid? EquipmentSerialId { get; set; }

        public byte? FireElement { get; set; }

        public short? FireResistance { get; set; }

        public short? HitRate { get; set; }

        public bool? HasSkin { get; set; }

        public short? HoldingVNum { get; set; }

        public short? HP { get; set; }

        public bool IsBreaked { get; set; }

        public bool? IsEmpty { get; set; }

        public bool? IsFixed { get; set; }

        public bool? IsPartnerEquipment { get; set; }

        public virtual Item Item { get; set; }

        public DateTime? ItemDeleteTime { get; set; }

        public short ItemVNum { get; set; }

        public byte? LightElement { get; set; }

        public short? LightResistance { get; set; }

        public short? MagicDefence { get; set; }

        public short? MaxElementRate { get; set; }

        public virtual ICollection<MinilandObject> MinilandObject { get; set; }

        public short? MP { get; set; }

        public short Rare { get; set; }

        public byte RuneAmount { get; set; }

        public short? ShellRarity { get; set; }

        public short? SlDamage { get; set; }

        public short? SlDefence { get; set; }

        public short? SlElement { get; set; }

        public short? SlHP { get; set; }

        [Index("IX_SlotAndType", 2, IsUnique = false, Order = 1)]
        public short Slot { get; set; }

        public byte? SpDamage { get; set; }

        public byte? SpDark { get; set; }

        public byte? SpDefence { get; set; }

        public byte? SpElement { get; set; }

        public byte? SpFire { get; set; }

        public byte? SpHP { get; set; }

        public byte? SpLevel { get; set; }

        public byte? SpLight { get; set; }

        public byte? SpStoneUpgrade { get; set; }

        public byte? SpWater { get; set; }

        [Index("IX_SlotAndType", 3, IsUnique = false, Order = 2)]
        public InventoryType Type { get; set; }

        public byte Upgrade { get; set; }

        public byte? WaterElement { get; set; }

        public short? WaterResistance { get; set; }

        public long? XP { get; set; }

        public byte Plus20Buff { get; set; }

        public int PartnerFusionProgress { get; set; }

        public int PartnerUpgradeLevel { get; set; }

        public int PartnerSkillPoint { get; set; }

        public int PartnerAttack { get; set; }

        public int PartnerDefence { get; set; }

        public int PartnerCritReduction { get; set; }

        public int PartnerHPMP { get; set; }

        public int PartnerFireRes { get; set; }

        public int PartnerWaterRes { get; set; }

        public int PartnerLightRes { get; set; }

        public int PartnerShadowRes { get; set; }

        public int OriginalWings { get; set; }

        public int WingBuff { get; set; }

        public int NewWings { get; set; }

        public bool ChangedWings { get; set; }

        public int FairyLevel { get; set; }

        #endregion
    }
}