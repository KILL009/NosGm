using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class SkillMapper
    {
        #region Methods

        public static bool ToSkill(SkillDTO input, Skill output)
        {
            if (input == null) return false;

            output.AttackAnimation = input.AttackAnimation;
            output.CastAnimation = input.CastAnimation;
            output.CastEffect = input.CastEffect;
            output.CastId = input.CastId;
            output.CastTime = input.CastTime;
            output.Class = input.Class;
            output.Cooldown = input.Cooldown;
            output.CPCost = input.CPCost;
            output.Duration = input.Duration;
            output.Effect = input.Effect;
            output.Element = input.Element;
            output.HitType = input.HitType;
            output.ItemVNum = input.ItemVNum;
            output.Level = input.Level;
            output.LevelMinimum = input.LevelMinimum;
            output.MinimumAdventurerLevel = input.MinimumAdventurerLevel;
            output.MinimumArcherLevel = input.MinimumArcherLevel;
            output.MinimumMagicianLevel = output.MinimumMagicianLevel;
            output.MinimumSwordmanLevel = input.MinimumSwordmanLevel;
            output.MpCost = input.MpCost;
            output.Name = input.Name;
            output.Price = input.Price;
            output.Range = input.Range;
            output.SkillType = input.SkillType;
            output.SkillVNum = input.SkillVNum;
            output.TargetRange = input.TargetRange;
            output.TargetType = input.TargetType;
            output.Type = input.Type;
            output.UpgradeSkill = input.UpgradeSkill;
            output.UpgradeType = input.UpgradeType;

            return true;
        }

        public static bool ToSkillDTO(Skill input, SkillDTO output)
        {
            if (input == null) return false;

            output.AttackAnimation = input.AttackAnimation;
            output.CastAnimation = input.CastAnimation;
            output.CastEffect = input.CastEffect;
            output.CastId = input.CastId;
            output.CastTime = input.CastTime;
            output.Class = input.Class;
            output.Cooldown = input.Cooldown;
            output.CPCost = input.CPCost;
            output.Duration = input.Duration;
            output.Effect = input.Effect;
            output.Element = input.Element;
            output.HitType = input.HitType;
            output.ItemVNum = input.ItemVNum;
            output.Level = input.Level;
            output.LevelMinimum = input.LevelMinimum;
            output.MinimumAdventurerLevel = input.MinimumAdventurerLevel;
            output.MinimumArcherLevel = input.MinimumArcherLevel;
            output.MinimumMagicianLevel = input.MinimumMagicianLevel;
            output.MinimumSwordmanLevel = input.MinimumSwordmanLevel;
            output.MpCost = input.MpCost;
            output.Name = input.Name;
            output.Price = input.Price;
            output.Range = input.Range;
            output.SkillType = input.SkillType;
            output.SkillVNum = input.SkillVNum;
            output.TargetRange = input.TargetRange;
            output.TargetType = input.TargetType;
            output.Type = input.Type;
            output.UpgradeSkill = input.UpgradeSkill;
            output.UpgradeType = input.UpgradeType;

            return true;
        }

        #endregion
    }
}