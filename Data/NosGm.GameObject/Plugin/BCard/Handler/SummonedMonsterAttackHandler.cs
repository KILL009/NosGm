using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SummonedMonsterAttackHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SummonedMonsterAttack;

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

            if (SubType == (byte)AdditionalTypes.SummonedMonsterAttack.Invisible)
            {
                if (target.Character is Character chara)
                {
                    chara.Invisible = true;
                    chara.Mates.Where(s => s.IsTeamMember).ToList().ForEach(s => chara.Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                    chara.Session.CurrentMapInstance?.Broadcast(chara.GenerateInvisible());
                }
            }
        }
    }
}
