using Game.Configuration.BCards;
using Frostvein.Domain;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject._plugins.BCards.Handler
{
    public class HPMPHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.HPMP;

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
            var FirstData = evnt.BCard.FirstData;
            var casterLevel = evnt.CasterLevel;
            var delaytime = evnt.DelayTime;
            var duration = evnt.Duration;

            if (SubType == (byte)AdditionalTypes.HPMP.RestoreDecreasedHP)
            {
                //120+(120�(50%))
                var missingHp = target.HpMax - target.Hp;
                int percentage = (int)(missingHp - ((missingHp * firstData) / 100));
                int bonus = missingHp - percentage;
                bool change = false;

                if (target.Hp + bonus <= target.HPLoad())
                {
                    target.Hp += bonus;
                    change = true;
                }
                else
                {
                    bonus = (int)target.HPLoad() - target.Hp;
                    target.Hp = (int)target.HPLoad();
                    change = true;
                }
                if (change)
                {
                    target.MapInstance?.Broadcast(target.GenerateRc(bonus));
                    target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                }
            }
            else if (SubType == (byte)AdditionalTypes.HPMP.RestoreDecreasedMP)
            {
                //120+(120�(50%))
                var missingHp = target.MpMax - target.Mp;
                int percentage = (int)(missingHp - ((missingHp * firstData) / 100));
                int bonus = missingHp - percentage;
                bool change = false;

                if (target.Mp + bonus <= target.MPLoad())
                {
                    target.Mp += bonus;
                    change = true;
                }
                else
                {
                    bonus = (int)target.MPLoad() - target.Mp;
                    target.Hp = (int)target.MPLoad();
                    change = true;
                }
                if (change)
                {
                    target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                }
            }
            else if (SubType == (byte)AdditionalTypes.HPMP.DecreaseRemainingHP)
            {
                //120+(120�(50%))
                var missingHp = target.HpMax - target.Hp;
                int percentage = (int)(missingHp - ((missingHp * firstData) / 100));
                int bonus = missingHp - percentage;
                target.GetDamage(bonus, caster, true, true);
                target.MapInstance?.Broadcast(target.GenerateDm(bonus));
                target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
            }
            else if (SubType == (byte)AdditionalTypes.HPMP.DecreaseRemainingMP)
            {
                var bonus = (int)(target.Mp * firstData / 100D);
                var change = false;
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
                    target.Character?.Session?.SendPacket(target.Character?.GenerateStat());
                }
            }
            else
            {
                var bonus = 0;
                var change = false;
                if (evnt.BCard.IsLevelScaled)
                {
                    bonus = casterLevel * (firstData + 1);
                }
                else if (evnt.BCard.IsLevelDivided)
                {
                    bonus = casterLevel / (firstData + 1);
                }
                else
                {
                    bonus = firstData;
                }

                void HPMPAction()
                {
                    if (target.Hp > 0)
                    {
                        switch (SubType)
                        {
                            case (byte)AdditionalTypes.HPMP.HPRestored:

                                if (target.Hp + bonus <= target.HPLoad())
                                {
                                    target.Hp += bonus;
                                    change = true;
                                }
                                else
                                {
                                    bonus = (int)target.HPLoad() - target.Hp;
                                    target.Hp = (int)target.HPLoad();
                                    change = true;
                                }

                                if (change)
                                {
                                    target.MapInstance?.Broadcast(target.GenerateRc(bonus));
                                    target.Character?.Session?.SendPacket(target.Character
                                        ?.GenerateStat());
                                }

                                break;

                            case (byte)AdditionalTypes.HPMP.HPReduced:

                                if (target.Hp - bonus > 1)
                                {
                                    target.Hp -= bonus;
                                    change = true;
                                }
                                else
                                {
                                    if (target.Hp != 1)
                                    {
                                        bonus = target.Hp - 1;
                                        target.Hp = 1;
                                        change = true;
                                    }
                                }

                                if (change)
                                {
                                    target.MapInstance?.Broadcast(target.GenerateDm(bonus));
                                    target.Character?.Session?.SendPacket(target.Character
                                        ?.GenerateStat());
                                }

                                break;

                            case (byte)AdditionalTypes.HPMP.MPRestored:
                                if (target.Mp + bonus <= target.MPLoad())
                                {
                                    target.Mp += bonus;
                                    change = true;
                                }
                                else
                                {
                                    bonus = (int)target.MPLoad() - target.Mp;
                                    target.Mp = (int)target.MPLoad();
                                    change = true;
                                }

                                if (change)
                                {
                                    target.Character?.Session?.SendPacket(target.Character
                                        ?.GenerateStat());
                                }

                                break;

                            case (byte)AdditionalTypes.HPMP.MPReduced:
                                if (target.Mp - bonus > 1)
                                {
                                    target.Mp -= bonus;
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
                                    target.Character?.Session?.SendPacket(target.Character
                                        ?.GenerateStat());
                                }

                                break;
                        }
                    }
                }

                HPMPAction();
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
                                HPMPAction();
                            }
                        });
                    target.BCardDisposables[BCardId] = bcardDisposable;
                }
            }
        }
    }
}