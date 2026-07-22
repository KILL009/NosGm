using Game.Configuration.BCards;
using NosGm.Domain;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class RecoveryAndDamagePercentHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.RecoveryAndDamagePercent;

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
            var IsLevelDivided = evnt.BCard.IsLevelDivided;

            if (CardId.HasValue && CardId.Value == 621)
            {
                // really ? NosGm fdp ?
                if (caster.Character != null && target.MapMonster != null)
                {
                    return;
                }
            }

            var bonus = 0;
            var change = false;
            if (IsLevelDivided)
            {
                bonus = (int)(casterLevel / firstData * (target.HPLoad() / 100));
            }
            else
            {
                bonus = (int)(firstData * (target.HPLoad() / 100));
            }

            void RecoveryAndDamagePercentAction()
            {
                if (target.Hp > 0)
                {
                    switch (SubType)
                    {
                        case (byte)AdditionalTypes.RecoveryAndDamagePercent.HPRecovered:

                            if (target.Hp >= target.HPLoad()) return;

                            if (target.Hp + bonus < target.HPLoad())
                            {
                                target.Hp += bonus;
                                change = true;
                            }
                            else if (target.Hp + bonus >= (int)target.HPLoad())
                            {
                                bonus = (int)target.HPLoad() - target.Hp;
                                target.Hp = (int)target.HPLoad();
                                change = true;
                            }

                            if (change)
                            {
                                target.MapInstance?.Broadcast(target.GenerateRc(bonus));
                                target.Character?.Session?.SendPacket(
                                    target.Character?.GenerateStat());
                            }

                            break;

                        case (byte)AdditionalTypes.RecoveryAndDamagePercent.HPReduced:
                            bonus = target.GetDamage(bonus, caster, true, true);
                            if (bonus > 0)
                            {
                                target.MapInstance?.Broadcast(target.GenerateDm(bonus));
                                target.Character?.Session?.SendPacket(
                                    target.Character?.GenerateStat());
                            }

                            break;

                        case (byte)AdditionalTypes.RecoveryAndDamagePercent.MPRecovered:
                            if (target.Mp + bonus < target.MPLoad())
                            {
                                target.Mp += bonus;
                                change = true;
                            }
                            else
                            {
                                if (target.Mp != (int)target.MPLoad())
                                {
                                    bonus = (int)target.MPLoad() - target.Mp;
                                    target.Mp = (int)target.MPLoad();
                                    change = true;
                                }
                            }

                            if (change)
                            {
                                target.Character?.Session?.SendPacket(
                                    target.Character?.GenerateStat());
                            }

                            break;

                        case (byte)AdditionalTypes.RecoveryAndDamagePercent.MPReduced:
                            if (target.Mp - bonus > 1)
                            {
                                target.DecreaseMp(bonus);
                                change = true;
                            }
                            else
                            {
                                if (target.Mp != 1)
                                {
                                    bonus = target.Mp - 1;
                                    target.Mp = 1;
                                    change = true;
                                }
                            }

                            if (change)
                            {
                                target.Character?.Session?.SendPacket(
                                    target.Character?.GenerateStat());
                            }

                            break;
                    }
                }
            }

            if (ThirdData > 0 && CastType == 0)
            {
                RecoveryAndDamagePercentAction();
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

                        if (target != null &&
                            ((target.Character != null && !target.Character.IsDisposed)
                             || (target.Mate != null)
                             || (target.MapMonster != null)
                             || (target.MapNpc != null)))
                        {
                            RecoveryAndDamagePercentAction();
                        }
                        else
                        {
                            bcardDisposable.Dispose();
                        }
                    });
                target.BCardDisposables[BCardId] = bcardDisposable;
            }
        }
    }
}
