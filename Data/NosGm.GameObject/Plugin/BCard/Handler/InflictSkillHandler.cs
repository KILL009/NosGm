using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class InflictSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.InflictSkill;

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

            Character user = caster.Character ?? target.Character;

            if (user == null)
            {
                return;
            }

            //if (SubType.Equals((byte)AdditionalTypes.InflictSkill.RageBarIncreaseByPercent) ||
            //SubType.Equals((byte)AdditionalTypes.InflictSkill.IncreaseRageBarEeverySec))
            //{

            //    void ActionRage()
            //    {
            //        if (user == null) return;
            //        user.AddUltimatePoints((short)FirstData);
            //        user.Session.SendPacket(user.GenerateFtPtPacket(true));
            //        user.AddSwordManBuffs();
            //    }

            //    ActionRage();
            //    if (ThirdData > 0)
            //    {
            //        IDisposable bcardDisposable = null;
            //        bcardDisposable = Observable
            //            .Interval(TimeSpan.FromSeconds(ThirdData * 2))
            //            .Subscribe(s =>
            //            {
            //                if (target.BCardDisposables[BCardId] != bcardDisposable)
            //                {
            //                    bcardDisposable.Dispose();
            //                    return;
            //                }
            //                if (target != null)
            //                {
            //                    ActionRage();
            //                }
            //            });
            //        target.BCardDisposables[BCardId] = bcardDisposable;
            //    }
            //}
        }
    }
}
