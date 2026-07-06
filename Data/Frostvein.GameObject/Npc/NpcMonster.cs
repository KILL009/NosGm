using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;

namespace Frostvein.GameObject
{
    public class NpcMonster : NpcMonsterDTO
    {
        #region Instantiation

        public NpcMonster()
        {
        }

        public NpcMonster(NpcMonsterDTO input)
        {
            AmountRequired = input.AmountRequired;
            AttackClass = input.AttackClass;
            AttackUpgrade = input.AttackUpgrade;
            BasicArea = input.BasicArea;
            BasicCooldown = input.BasicCooldown;
            BasicRange = input.BasicRange;
            BasicSkill = input.BasicSkill;
            Catch = input.Catch;
            CloseDefence = input.CloseDefence;
            Concentrate = input.Concentrate;
            CriticalChance = input.CriticalChance;
            CriticalRate = input.CriticalRate;
            DamageMaximum = input.DamageMaximum;
            DamageMinimum = input.DamageMinimum;
            DarkResistance = input.DarkResistance;
            DefenceDodge = input.DefenceDodge;
            DefenceUpgrade = input.DefenceUpgrade;
            DistanceDefence = input.DistanceDefence;
            DistanceDefenceDodge = input.DistanceDefenceDodge;
            Element = input.Element;
            ElementRate = input.ElementRate;
            FireResistance = input.FireResistance;
            HeroLevel = input.HeroLevel;
            HeroXp = input.HeroXp;
            IsHostile = input.IsHostile;
            IsSealedVessel = input.IsSealedVessel;
            JobXP = input.JobXP;
            Level = input.Level;
            LightResistance = input.LightResistance;
            MagicDefence = input.MagicDefence;
            MaxHP = input.MaxHP;
            MaxMP = input.MaxMP;
            MonsterType = input.MonsterType;
            Name = input.Name;
            NoAggresiveIcon = input.NoAggresiveIcon;
            NoticeRange = input.NoticeRange;
            GroupAttack = input.GroupAttack;
            NpcMonsterVNum = input.NpcMonsterVNum;
            OriginalNpcMonsterVNum = input.OriginalNpcMonsterVNum;
            Race = input.Race;
            RaceType = input.RaceType;
            RespawnTime = input.RespawnTime;
            Speed = input.Speed;
            VNumRequired = input.VNumRequired;
            WaterResistance = input.WaterResistance;
            XP = input.XP;
            EvolvePet = input.EvolvePet;
            Tameable = input.Tameable;
        }

        #endregion

        #region Properties

        public List<BCard> BCards { get; set; }

        public List<DropDTO> Drops { get; set; }

        public short FirstX { get; set; }

        public short FirstY { get; set; }

        public DateTime LastEffect { get; private set; }

        public DateTime LastMove { get; private set; }

        public List<NpcMonsterSkill> Skills { get; set; }

        public List<TeleporterDTO> Teleporters { get; set; }

        #endregion

        #region Methods

        public string GenerateEInfo()
        {
            return
                $"e_info 10 {(OriginalNpcMonsterVNum > 0 ? OriginalNpcMonsterVNum : NpcMonsterVNum)} {Level} {Element} {AttackClass} {ElementRate} {AttackUpgrade} {DamageMinimum} {DamageMaximum} {Concentrate} {CriticalChance} {CriticalRate} {DefenceUpgrade} {CloseDefence} {DefenceDodge} {DistanceDefence} {DistanceDefenceDodge} {MagicDefence} {FireResistance} {WaterResistance} {LightResistance} {DarkResistance} {MaxHP} {MaxMP} -1 {Name.Replace(' ', '^')}";
        }

        public float GetRes(int skillelement)
        {
            switch (skillelement)
            {
                case 0:
                    return FireResistance / 100;

                case 1:
                    return WaterResistance / 100;

                case 2:
                    return LightResistance / 100;

                case 3:
                    return DarkResistance / 100;

                default:
                    return 0f;
            }
        }

        /// <summary>
        ///     Intializes the GameObject, will be injected by AutoMapper after Entity -&gt; GO mapping
        /// </summary>
        public void Initialize()
        {
            Teleporters = ServerManager.Instance.GetTeleportersByNpcVNum(NpcMonsterVNum);
            Drops = ServerManager.Instance.GetDropsByMonsterVNum(NpcMonsterVNum);
            LastEffect = LastMove = DateTime.Now;
            Skills = ServerManager.Instance.GetNpcMonsterSkillsByMonsterVNum(OriginalNpcMonsterVNum > 0
                ? OriginalNpcMonsterVNum
                : NpcMonsterVNum);
            if (Skills.Count == 0 && OriginalNpcMonsterVNum > 0)
                Skills = ServerManager.Instance.GetNpcMonsterSkillsByMonsterVNum(NpcMonsterVNum);
        }

        #endregion
    }
}