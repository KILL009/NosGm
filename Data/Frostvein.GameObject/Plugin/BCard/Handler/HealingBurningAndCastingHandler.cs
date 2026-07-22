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
            var secondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var skill = evnt.Skill;
            var thirdData = evnt.BCard.ThirdData;
            var cardId = evnt.BCard.CardId;
            var skillVNum = evnt.BCard.SkillVNum;
            var bCardId = evnt.BCard.BCardId;
            var subType = evnt.BCard.SubType;
            var casterLevel = evnt.CasterLevel;
            var delayTime = evnt.DelayTime;
            var duration = evnt.Duration;

            var castType = evnt.BCard.CastType;
            var isLevelScaled = evnt.BCard.IsLevelScaled;
            var isLevelDivided = evnt.BCard.IsLevelDivided;

            var amount = 0;

            void HealingBurningAndCastingAction()
            {
                if (target.Hp < 1 || target.MapInstance == null)
                {
                    return;
                }

                if (isLevelDivided)
                {
                    amount = casterLevel / (firstData + 1);
                }
                else if (isLevelScaled)
                {
                    amount = casterLevel * (firstData + 1);
                }
                else
                {
                    amount = firstData;
                }

                switch (subType)
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

            var interval = thirdData > 0 ? thirdData * 2 : castType * 2;

            if (cardId.HasValue && interval > 0)
            {
                int disposableKey = cardId.Value;
                IDisposable bcardDisposable = null;
                bcardDisposable = Observable.Interval(TimeSpan.FromSeconds(interval))
                    .Subscribe(s =>
                    {
                        if (target.BCardDisposables[disposableKey] != bcardDisposable)
                        {
                            bcardDisposable.Dispose();
                            return;
                        }

                        if (target != null)
                        {
                            HealingBurningAndCastingAction();
                        }
                    });
                target.BCardDisposables[disposableKey] = bcardDisposable;
            }
        }
    }
}
