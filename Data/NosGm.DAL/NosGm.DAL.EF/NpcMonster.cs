using NosGm.DAL.EF.Entities;
using NosGm.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NosGm.DAL.EF
{
    public class NpcMonster
    {
        #region Instantiation

        public NpcMonster()
        {
            Drop = new HashSet<Drop>();
            MapMonster = new HashSet<MapMonster>();
            MapNpc = new HashSet<MapNpc>();
            NpcMonsterSkill = new HashSet<NpcMonsterSkill>();
            Mate = new HashSet<Mate>();
            BCards = new HashSet<BCard>();
            MonsterType = MonsterType.Unknown;
        }

        #endregion

        #region Properties

        public short AmountRequired { get; set; }

        public byte AttackClass { get; set; }

        public byte AttackUpgrade { get; set; }

        public byte BasicArea { get; set; }

        public short BasicCooldown { get; set; }

        public byte BasicRange { get; set; }

        public short BasicSkill { get; set; }

        public virtual ICollection<BCard> BCards { get; set; }

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

        public virtual ICollection<Drop> Drop { get; set; }

        public short Element { get; set; }

        public short ElementRate { get; set; }

        public short FireResistance { get; set; }

        public int HeroLevel { get; set; }

        public int HeroXP { get; set; }

        public bool IsHostile { get; set; }

        public bool IsSealedVessel { get; set; }

        public int JobXP { get; set; }

        public int Level { get; set; }

        public short LightResistance { get; set; }

        public short MagicDefence { get; set; }

        public virtual ICollection<MapMonster> MapMonster { get; set; }

        public virtual ICollection<MapNpc> MapNpc { get; set; }

        public virtual ICollection<Mate> Mate { get; set; }

        public int MaxHP { get; set; }

        public int MaxMP { get; set; }

        public MonsterType MonsterType { get; set; }

        [MaxLength(255)] public string Name { get; set; }

        public bool NoAggresiveIcon { get; set; }

        public short NoticeRange { get; set; }

        public GroupAttackType GroupAttack { get; set; }

        public virtual ICollection<NpcMonsterSkill> NpcMonsterSkill { get; set; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
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