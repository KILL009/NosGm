using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class AttackPowerHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.AttackPower;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var skill = evnt.Skill;
            var SubType = evnt.BCard.SubType;

            if (SubType == (byte)AdditionalTypes.AttackPower.AllAttacksIncreased ||
                SubType == (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased ||
                SubType == (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased ||
                SubType == (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased)
            {
                if (target.Character != null && caster.Character != null && target.Character == caster.Character)
                {
                    if (skill != null && skill.UpgradeSkill == 0)
                    {
                        List<CharacterSkill> skills = target.Character.GetSkills();

                        target.Character.ChargeValue = skills.Where(s => s.SkillVNum == skill.SkillVNum).Sum(s => s.GetSkillBCards().Sum(b => b.FirstData));
                        target.AddBuff(new Buff(0, target.Level, false), target);
                    }
                }
            }
        }
    }
}
