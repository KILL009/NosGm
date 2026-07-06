using Game.Configuration.BCards;
using Frostvein.Domain;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class HealingBurningAndCastingHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.HealingBurningAndCasting;

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

            var CastType = evnt.BCard.CastType;
            var IsLevelScaled = evnt.BCard.IsLevelScaled;
            var IsLevelDivided = evnt.BCard.IsLevelDivided;

            var amount = 0;

            void HealingBurningAndCastingAction()
            {
                if (target.Hp < 1
                    || target.MapInstance == null)
                {
                    return;
                }

                if (IsLevelDivided)
                {
                    amount = casterLevel / (firstData + 1);
                }
                else if (IsLevelDivided && IsLevelScaled)
                {
                    amount = casterLevel / (firstData + 1);
                }
                else if (IsLevelScaled)
                {
                    amount = casterLevel * (firstData + 1);
                }
                else
                {
                    amount = firstData;
                }

                switch (SubType)
                {
                    case (byte)AdditionalTypes.HealingBurningAndCasting.RestoreHP:

                        if (target.Hp + amount > target.HpMax)
                        {
                            amount = target.HpMax - target.Hp;
                        }

                        if (amount > 0)
                        {
                            target.Hp += amount;

                            target.MapInstance.Broadcast(target.GenerateRc(amount));
                        }
                        else if (amount < 0)
                        {
                            amount = target.GetDamage(amount, caster, true, true);

                            target.MapInstance.Broadcast(target.GenerateDm(amount));
                        }

                        break;

                    case (byte)AdditionalTypes.HealingBurningAndCasting.RestoreMP:

                        if (target.Mp + amount > target.MpMax)
                        {
                            amount = target.MpMax - target.Mp;
                        }

                        target.Mp += amount;

                        break;

                    case (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseHP:

                        target.Hp = target.Hp - amount <= 0 ? 1 : target.Hp - amount;
                        target.MapInstance?.Broadcast(target.GenerateDm(amount));

                        break;

                    case (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseMP:

                        target.Mp = target.Mp - amount <= 0 ? 1 : target.Mp - amount;

                        break;
                }

                target?.Character?.Session?.SendPacket(target.Character?.GenerateStat());
            }

            HealingBurningAndCastingAction();

            var interval = ThirdData > 0 ? ThirdData * 2 : CastType * 2;

            if (CardId != null && interval > 0)
            {
                IDisposable bcardDisposable = null;
                bcardDisposable = Observable.Interval(TimeSpan.FromSeconds(interval))
                    .Subscribe(s =>
                    {
                        if (target.BCardDisposables[BCardId] != bcardDisposable)
                        {
                            bcardDisposable.Dispose();
                            return;
                        }

                        if (target != null)
                        {
                            HealingBurningAndCastingAction();
                        }
                    });
                target.BCardDisposables[BCardId] = bcardDisposable;
            }
        }
    }
}
