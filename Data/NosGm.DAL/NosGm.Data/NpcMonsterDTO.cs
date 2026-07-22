using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public class NpcMonsterDTO
    {
        #region Properties

        public short AmountRequired { get; set; }

        public byte AttackClass { get; set; }

        public byte AttackUpgrade { get; set; }

        public byte BasicArea { get; set; }

        public short BasicCooldown { get; set; }

        public byte BasicRange { get; set; }

        public short BasicSkill { get; set; }

        public bool Catch { get; set; }

        public short CloseDefence { get; set; }

        public short Concentrate { get; set; }

        public byte CriticalChance { get; set; }

        public short CriticalRate { get; set; }

        public short DamageMaximum { get; set; }

        public short DamageMinimum { get; set; }

        public short DarkResistance { get; set; }

        public short DefenceDodge { get; set; }

        public byte DefenceUpgrade { get; set; }

        public short DistanceDefence { get; set; }

        public short DistanceDefenceDodge { get; set; }

        public short Element { get; set; }

        public short ElementRate { get; set; }

        public short FireResistance { get; set; }

        public int HeroLevel { get; set; }

        public int HeroXp { get; set; }

        public bool IsHostile { get; set; }

        public bool IsSealedVessel { get; set; }

        public int JobXP { get; set; }

        public int Level { get; set; }

        public short LightResistance { get; set; }

        public short MagicDefence { get; set; }

        public int MaxHP { get; set; }

        public int MaxMP { get; set; }

        public MonsterType MonsterType { get; set; }

        public string Name { get; set; }

        public bool NoAggresiveIcon { get; set; }

        public short NoticeRange { get; set; }

        public GroupAttackType GroupAttack { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short OriginalNpcMonsterVNum { get; set; }

        public short Race { get; set; }

        public short RaceType { get; set; }

        public int RespawnTime { get; set; }

        public byte Speed { get; set; }

        public short VNumRequired { get; set; }

        public short WaterResistance { get; set; }

        public int XP { get; set; }

        public short EvolvePet { get; set; }

        public byte Tameable { get; set; }

        #endregion
    }
}