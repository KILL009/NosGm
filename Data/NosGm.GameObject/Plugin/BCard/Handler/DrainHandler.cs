using Game.Configuration.BCards;
using NosGm.Domain;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class DrainHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Drain;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var ThirdData = evnt.BCard.ThirdData;
            var BCardId = evnt.BCard.BCardId;
            var SubType = evnt.BCard.SubType;
            var casterLevel = evnt.CasterLevel;
            var IsLevelScaled = evnt.BCard.IsLevelScaled;

            void DrainAction()
            {
                if (target.Hp > 0 && caster.Hp > 0)
                {
                    if (SubType == (byte)AdditionalTypes.Drain.TransferEnemyHP)
                    {
                        int bonus = 0;
                        bool senderChange = false;
                        if (IsLevelScaled)
                        {
                            bonus = casterLevel * firstData;
                        }
                        else
                        {
                            bonus = firstData;
                        }
                        bonus = target.GetDamage(bonus, caster, true, true);
                        if (bonus > 0)
                        {
                            target.MapInstance?.Broadcast(target.GenerateDm(bonus));
                            target.Character?.Session?.SendPacket(target.Character?.GenerateStat());

                            if (caster.Hp + bonus <= caster.HPLoad())
                            {
                                caster.Hp += bonus;
                                senderChange = true;
                            }
                            else
                            {
                                bonus = (int)caster.HPLoad() - caster.Hp;
                                caster.Hp = (int)caster.HPLoad();
                                senderChange = true;
                            }
                            if (senderChange)
                            {
                                caster.MapInstance?.Broadcast(caster.GenerateRc(bonus));
                                caster.Character?.Session?.SendPacket(caster.Character?.GenerateStat());
                            }
                        }
                    }
                }
            }
            DrainAction();
            if (ThirdData > 0)
            {
                IDisposable bcardDisposable = null;
                bcardDisposable = Observable
                    .Interval(TimeSpan.FromSeconds(ThirdData * 2))
                    .Subscribe(s =>
                    {
                        if (target.BCardDisposables[BCardId] != bcardDisposable)
                        {
                            bcardDisposable.Dispose();
                            return;
                        }
                        if (target != null)
                        {
                            DrainAction();
                        }
                    });
                target.BCardDisposables[BCardId] = bcardDisposable;
            }
        }
    }
}
