using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SniperAttackHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SniperAttack;

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

            if (target == caster)
            {
                return;
            }

            if (SubType.Equals((byte)AdditionalTypes.SniperAttack.ChanceCausing))
            {
                if (ServerManager.RandomNumber() < firstData)
                {
                    Buff ChanceCausingBuff = new Buff((short)SecondData, casterLevel);
                    if (ChanceCausingBuff.Card.BuffType == BuffType.Good || ChanceCausingBuff.Card.BuffType == BuffType.Neutral)
                    {
                        caster.AddBuff(ChanceCausingBuff, caster);
                    }
                    else
                    {
                        target.AddBuff(ChanceCausingBuff, caster);
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.SniperAttack.ProduceChance))
            {
                if (caster.Buffs.Any(s => s.Card.BCards.Any(b => b.Type == (byte)BCardType.CardType.FalconSkill && (b.SubType == (byte)AdditionalTypes.FalconSkill.Hide || b.SubType == (byte)AdditionalTypes.FalconSkill.Ambush))))
                {
                    if (ServerManager.RandomNumber() < firstData)
                    {
                        Buff ProduceChanceBuff = new Buff((short)SecondData, casterLevel);
                        if (ProduceChanceBuff.Card.BuffType == BuffType.Good || ProduceChanceBuff.Card.BuffType == BuffType.Neutral)
                        {
                            caster.AddBuff(ProduceChanceBuff, caster);
                        }
                        else
                        {
                            target.AddBuff(ProduceChanceBuff, caster);
                        }
                    }
                }
            }
        }
    }
}
