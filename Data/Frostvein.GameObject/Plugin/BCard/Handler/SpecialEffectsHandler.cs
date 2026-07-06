using Game.Configuration.BCards;
using Frostvein.Domain;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SpecialEffectHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SpecialEffects;

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

            if (!SubType.Equals((byte)AdditionalTypes.SpecialEffects.ShadowAppears))
            {
                return;
            }
            target.MapInstance.Broadcast($"guri 0 {(short)target.UserType} {target.MapEntityId} {firstData} {SecondData}");
        }
    }
}
