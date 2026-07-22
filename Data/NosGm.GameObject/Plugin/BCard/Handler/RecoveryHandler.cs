using Game.Configuration.BCards;
using NosGm.Domain;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class RecoveryHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Recovery;

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

            if (SubType == (byte)AdditionalTypes.Recovery.IncreaseMaxHpByPercent)
            {
                caster.HPLoad();

                if (caster.Character != null)
                {
                    caster.Character.Session.SendPacket(caster.Character.GenerateStat());
                }
            }
        }
    }
}
