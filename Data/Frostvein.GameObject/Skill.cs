using Frostvein.Data;
using System;
using System.Collections.Generic;

namespace Frostvein.GameObject
{
    public class Skill : SkillDTO
    {
        #region Instantiation

        public Skill()
        {
            Combos = new List<ComboDTO>();
            BCards = new List<BCard>();
        }

        public Skill(SkillDTO input) : this()
        {
            AttackAnimation = input.AttackAnimation;
            CastAnimation = input.CastAnimation;
            CastEffect = input.CastEffect;
            CastId = input.CastId;
            CastTime = input.CastTime;
            Class = input.Class;
            Cooldown = input.Cooldown;
            CPCost = input.CPCost;
            Duration = input.Duration;
            Effect = input.Effect;
            Element = input.Element;
            HitType = input.HitType;
            ItemVNum = input.ItemVNum;
            Level = input.Level;
            LevelMinimum = input.LevelMinimum;
            MinimumAdventurerLevel = input.MinimumAdventurerLevel;
            MinimumArcherLevel = input.MinimumArcherLevel;
            MinimumMagicianLevel = input.MinimumMagicianLevel;
            MinimumSwordmanLevel = input.MinimumSwordmanLevel;
            MpCost = input.MpCost;
            Name = input.Name;
            Price = input.Price;
            Range = input.Range;
            SkillType = input.SkillType;
            SkillVNum = input.SkillVNum;
            TargetRange = input.TargetRange;
            TargetType = input.TargetType;
            Type = input.Type;
            UpgradeSkill = input.UpgradeSkill;
            UpgradeType = input.UpgradeType;
        }

        #endregion

        #region Properties

        public List<BCard> BCards { get; set; }

        public List<ComboDTO> Combos { get; set; }

        public PartnerSkill PartnerSkill { get; set; }

        public NpcMonsterSkill PetSkill { get; set; }

        public DateTime LastSkillUse { get; set; }

        #endregion

        #region Method

        public bool CanBeUsed() => this != null && LastSkillUse.AddMilliseconds(this.Cooldown * 100) < DateTime.Now;

        #endregion
    }
}