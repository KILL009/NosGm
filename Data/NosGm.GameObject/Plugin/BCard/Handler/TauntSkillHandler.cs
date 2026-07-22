using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class TauntSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.TauntSkill;

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

            if (SubType.Equals((byte)AdditionalTypes.TauntSkill.TauntWhenNormal))
            {
                if (!target.Buffs.Any(s => s.Card.CardId == 500) && ServerManager.RandomNumber() < FirstData)
                {
                    target.AddBuff(new Buff((short)SecondData, casterLevel), caster);
                }
            }
            if (SubType.Equals((byte)AdditionalTypes.TauntSkill.TauntWhenKnockdown))
            {
                if (target.Buffs.Any(s => s.Card.CardId == 500) && ServerManager.RandomNumber() < FirstData)
                {
                    target.AddBuff(new Buff((short)SecondData, casterLevel), caster);
                }
            }

        }
    }
}
