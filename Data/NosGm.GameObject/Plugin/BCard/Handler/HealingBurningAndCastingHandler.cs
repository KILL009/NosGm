using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Domain;
using System;
using System.Reactive.Linq;

namespace Game.Configuration
{
    public class HealingBurningAndCastingHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.HealingBurningAndCasting;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var firstData = evnt.FirstData;
            var thirdData = evnt.BCard.ThirdData;
            var cardId = evnt.BCard.CardId;
            var bCardId = evnt.BCard.BCardId;
            var subType = evnt.BCard.SubType;
            var casterLevel = evnt.CasterLevel;
            var castType = evnt.BCard.CastType;
            var isLevelScaled = evnt.BCard.IsLevelScaled;
            var isLevelDivided = evnt.BCard.IsLevelDivided;

            var amount = 0;

            void RefreshTargetStatus()
            {
                target.Character?.Session?.SendPacket(target.Character.GenerateStat());
                if (target.Mate?.Owner?.Session != null)
                {
                    target.Mate.Owner.Session.SendPackets(target.Mate.Owner.GeneratePst());
                }
            }

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
                        target.MapInstance.Broadcast(target.GenerateDm(amount));
                        break;

                    case (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseMP:
                        target.Mp = target.Mp - amount <= 0 ? 1 : target.Mp - amount;
                        break;
                }

                RefreshTargetStatus();
            }

            HealingBurningAndCastingAction();

            var interval = thirdData > 0 ? thirdData * 2 : castType * 2;
            if (!cardId.HasValue || interval <= 0)
            {
                return;
            }

            // BattleEntity.RemoveBuff disposes every periodic BCard by BCardId.
            // The old implementation stored this interval under CardId instead,
            // so poison/burning ticks could survive after the owning buff expired.
            int disposableKey = bCardId > 0 ? bCardId : cardId.Value;
            IDisposable bcardDisposable = null;
            bcardDisposable = Observable.Interval(TimeSpan.FromSeconds(interval))
                .Subscribe(_ =>
                {
                    if (target.BCardDisposables[disposableKey] != bcardDisposable)
                    {
                        bcardDisposable.Dispose();
                        return;
                    }

                    // Also stop defensively when the visible owning buff is gone.
                    // This prevents an orphan timer from continuing to damage a mate.
                    if (!target.HasBuff(cardId.Value))
                    {
                        bcardDisposable.Dispose();
                        if (target.BCardDisposables[disposableKey] == bcardDisposable)
                        {
                            target.BCardDisposables.Remove(disposableKey);
                        }

                        Logger.Info(
                            $"[MATE_DEBUFF] Result=StoppedOrphan Card={cardId.Value} " +
                            $"BCard={bCardId} TargetType={target.EntityType} Target={target.MapEntityId}");
                        RefreshTargetStatus();
                        return;
                    }

                    HealingBurningAndCastingAction();
                });
            target.BCardDisposables[disposableKey] = bcardDisposable;
        }
    }
}
