using Game.Configuration.BCards;
using Frostvein.Domain;
using Frostvein.GameObject;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SESpecialistHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SESpecialist;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var skill = evnt.Skill;
            var ThirdData = evnt.BCard.ThirdData;
            var CardId = evnt.BCard.CardId;
            var SkillVNum = evnt.BCard.SkillVNum;
            var BCardId = evnt.BCard.BCardId;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;
            var delaytime = evnt.DelayTime;
            var duration = evnt.Duration;

            if (SubType.Equals((byte)AdditionalTypes.SESpecialist.LowerHPStrongerEffect))
            {
                double hpPercentage = target.Hp / target.HPLoad() * 100;
                if (hpPercentage < 35)
                {
                    target.AddBuff(new Buff(274, casterLevel), caster);
                }
                else if (hpPercentage < 67)
                {
                    target.AddBuff(new Buff(273, casterLevel), caster);
                }
                else
                {
                    target.AddBuff(new Buff(272, casterLevel), caster);
                }
            }
        }
    }
}
