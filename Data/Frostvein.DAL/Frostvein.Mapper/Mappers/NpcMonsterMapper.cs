using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class NpcMonsterMapper
    {
        #region Methods

        public static bool ToNpcMonster(NpcMonsterDTO input, NpcMonster output)
        {
            if (input == null) return false;

            output.AmountRequired = input.AmountRequired;
            output.AttackClass = input.AttackClass;
            output.AttackUpgrade = input.AttackUpgrade;
            output.BasicArea = input.BasicArea;
            output.BasicCooldown = input.BasicCooldown;
            output.BasicRange = input.BasicRange;
            output.BasicSkill = input.BasicSkill;
            output.Catch = input.Catch;
            output.CloseDefence = input.CloseDefence;
            output.Concentrate = input.Concentrate;
            output.CriticalChance = input.CriticalChance;
            output.CriticalRate = input.CriticalRate;
            output.DamageMaximum = input.DamageMaximum;
            output.DamageMinimum = input.DamageMinimum;
            output.DarkResistance = input.DarkResistance;
            output.DefenceDodge = input.DefenceDodge;
            output.DefenceUpgrade = input.DefenceUpgrade;
            output.DistanceDefence = input.DistanceDefence;
            output.DistanceDefenceDodge = input.DistanceDefenceDodge;
            output.Element = input.Element;
            output.ElementRate = input.ElementRate;
            output.FireResistance = input.FireResistance;
            output.HeroLevel = input.HeroLevel;
            output.HeroXP = input.HeroXp;
            output.IsHostile = input.IsHostile;
            output.IsSealedVessel = input.IsSealedVessel;
            output.JobXP = input.JobXP;
            output.Level = input.Level;
            output.LightResistance = input.LightResistance;
            output.MagicDefence = input.MagicDefence;
            output.MaxHP = input.MaxHP;
            output.MaxMP = input.MaxMP;
            output.MonsterType = input.MonsterType;
            output.Name = input.Name;
            output.NoAggresiveIcon = input.NoAggresiveIcon;
            output.NoticeRange = input.NoticeRange;
            output.GroupAttack = input.GroupAttack;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.OriginalNpcMonsterVNum = input.OriginalNpcMonsterVNum;
            output.Race = input.Race;
            output.RaceType = input.RaceType;
            output.RespawnTime = input.RespawnTime;
            output.Speed = input.Speed;
            output.VNumRequired = input.VNumRequired;
            output.WaterResistance = input.WaterResistance;
            output.XP = input.XP;
            output.EvolvePet = input.EvolvePet;
            output.Tameable = input.Tameable;

            return true;
        }

        public static bool ToNpcMonsterDTO(NpcMonster input, NpcMonsterDTO output)
        {
            if (input == null) return false;

            output.AmountRequired = input.AmountRequired;
            output.AttackClass = input.AttackClass;
            output.AttackUpgrade = input.AttackUpgrade;
            output.BasicArea = input.BasicArea;
            output.BasicCooldown = input.BasicCooldown;
            output.BasicRange = input.BasicRange;
            output.BasicSkill = input.BasicSkill;
            output.Catch = input.Catch;
            output.CloseDefence = input.CloseDefence;
            output.Concentrate = input.Concentrate;
            output.CriticalChance = input.CriticalChance;
            output.CriticalRate = input.CriticalRate;
            output.DamageMaximum = input.DamageMaximum;
            output.DamageMinimum = input.DamageMinimum;
            output.DarkResistance = input.DarkResistance;
            output.DefenceDodge = input.DefenceDodge;
            output.DefenceUpgrade = input.DefenceUpgrade;
            output.DistanceDefence = input.DistanceDefence;
            output.DistanceDefenceDodge = input.DistanceDefenceDodge;
            output.Element = input.Element;
            output.ElementRate = input.ElementRate;
            output.FireResistance = input.FireResistance;
            output.HeroLevel = input.HeroLevel;
            output.HeroXp = input.HeroXP;
            output.IsHostile = input.IsHostile;
            output.IsSealedVessel = input.IsSealedVessel;
            output.JobXP = input.JobXP;
            output.Level = input.Level;
            output.LightResistance = input.LightResistance;
            output.MagicDefence = input.MagicDefence;
            output.MaxHP = input.MaxHP;
            output.MaxMP = input.MaxMP;
            output.MonsterType = input.MonsterType;
            output.Name = input.Name;
            output.NoAggresiveIcon = input.NoAggresiveIcon;
            output.NoticeRange = input.NoticeRange;
            output.GroupAttack = input.GroupAttack;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.OriginalNpcMonsterVNum = input.OriginalNpcMonsterVNum;
            output.Race = input.Race;
            output.RaceType = input.RaceType;
            output.RespawnTime = input.RespawnTime;
            output.Speed = input.Speed;
            output.VNumRequired = input.VNumRequired;
            output.WaterResistance = input.WaterResistance;
            output.XP = input.XP;
            output.EvolvePet = input.EvolvePet;
            output.Tameable = input.Tameable;

            return true;
        }

        #endregion
    }
}