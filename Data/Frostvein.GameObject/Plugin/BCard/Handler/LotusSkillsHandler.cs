using Game.Configuration.BCards;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using System.Threading.Tasks;


namespace Game.Configuration
{
    public class LotusSkillsHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.LotusSkills;

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

            switch (SubType)
            {
                case ((byte)AdditionalTypes.LotusSkills.DodgeAttackAndGenerateBuff):
                    {
                        target.AddBuff(new Buff((short)SecondData, target.Level), target);
                    }
                    break;
            }
        }
    }
}
