using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class LordHatusHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.LordHatus;

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

            if (caster.Character != null)
            {
                var attacker = caster.Character.Mates.Find(x => x.IsSunWolf);

                if (attacker == null || attacker.HasBuff(BCardType.CardType.SpecialAttack,
                    (byte)AdditionalTypes.SpecialAttack.NoAttack)) return;

                if (!(attacker.Hp > 0)) return;

                if (target == null) return;

                if (!attacker.BattleEntity.CanAttackEntity(target)) return;

                var skill2 = new NpcMonsterSkill
                {
                    SkillVNum = (short)SecondData
                };
                attacker.TargetHit(target, skill2);
            }
        }
    }
}
