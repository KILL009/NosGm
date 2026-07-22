using Game.Configuration.BCards;
using NosGm.Domain;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class BossMonstersSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.BossMonstersSkill;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;


            //if (SubType.Equals((byte)AdditionalTypes.BossMonstersSkill.GiveBuffToSunWolfAndOwner))
            //{
            //    if (caster.Character != null)
            //    {
            //        if (ServerManager.RandomNumber() < FirstData)
            //        {
            //            caster.Character.AddBuff(new Buff((short)SecondData, casterLevel), caster);
            //            var sunWo = caster.Character.Mates.Where(s => s.IsSunWolf).FirstOrDefault();
            //            if (sunWo != null)
            //            {
            //                sunWo.AddBuff(new Buff((short)SecondData, casterLevel), caster);
            //            }
            //        }
            //    }
            //}
        }
    }
}
