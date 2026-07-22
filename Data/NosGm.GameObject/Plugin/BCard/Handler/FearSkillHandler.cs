using Game.Configuration.BCards;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class FearSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.FearSkill;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;

            if (SubType.Equals((byte)AdditionalTypes.FearSkill.TimesUsed))
            {
                if (caster.Character != null)
                {
                    if ((FirstData == 1 && caster.Character.SkillComboCount == 3) || 
                        (FirstData == 2 && caster.Character.SkillComboCount == 5))
                    {
                        caster.AddBuff(new Buff((short)SecondData, casterLevel), caster);
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.FearSkill.AttackRangedIncreased))
            {
                if (target.Character != null)
                {
                    target.Character.Session.SendPacket($"bf_d {FirstData} 1");
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.FearSkill.MoveAgainstWill))
            {
                if (target.Character != null)
                {
                    target.Character.Session.SendPacket($"rv_m {target.MapEntityId} 1 1");
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.FearSkill.ProduceWhenAmbushe))
            {
                if (caster == target && (x != 0 || y != 0) && SecondData > 0)
                {
                    if (ServerManager.RandomNumber() < firstData)
                    {
                        caster.AddBuff(new Buff((short)SecondData, casterLevel), caster);
                    }
                }
            }

        }
    }
}
