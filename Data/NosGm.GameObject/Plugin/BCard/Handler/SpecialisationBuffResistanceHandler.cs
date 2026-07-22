using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SpecialisationBuffResistanceHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SpecialisationBuffResistance;

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
            if (SubType.Equals((byte)AdditionalTypes.SpecialisationBuffResistance.RemoveGoodEffects))
            {
                if (ServerManager.RandomNumber() < FirstData)
                {
                    target.DisableBuffs(BuffType.Good, SecondData + 1);
                }
            }
            if (SubType.Equals((byte)AdditionalTypes.SpecialisationBuffResistance.RemoveBadEffects))
            {
                if (ServerManager.RandomNumber() < -FirstData)
                {
                    target.DisableBuffs(BuffType.Bad, SecondData + 1);
                }
                if (target == caster && skill?.SkillVNum == 1098)
                {
                    if (target.BCardDisposables[BCardId] != null)
                    {
                        target.BCardDisposables[BCardId].Dispose();
                        target.BCardDisposables[BCardId] = null;
                        return;
                    }
                    else
                    {
                        int count = 0, count2 = 0;
                        IDisposable fiveSecs = null;
                        fiveSecs = Observable.Interval(TimeSpan.FromSeconds(2.5)).Subscribe(s =>
                        {
                            count++;
                            RemoveBadEffectsAction(true);
                            if (count == 2)
                            {
                                fiveSecs.Dispose();
                            }
                        });
                        IDisposable bcardDisposable = null;
                        bcardDisposable = Observable.Interval(TimeSpan.FromSeconds(2.5)).Subscribe(s =>
                        {
                            if (target.BCardDisposables[BCardId] != bcardDisposable)
                            {
                                bcardDisposable.Dispose();
                                return;
                            }
                            if (!target.Character.Session.HasCurrentMapInstance)
                            {
                                target.BCardDisposables[BCardId].Dispose();
                                target.BCardDisposables[BCardId] = null;
                                return;
                            }
                            count2++;
                            if (count2 > 2)
                            {
                                bool health = false;
                                if (target.Mp >= 144)
                                {
                                    health = true;
                                    target.DecreaseMp(144);
                                }
                                RemoveBadEffectsAction(health);
                            }
                        });
                        target.BCardDisposables[BCardId] = bcardDisposable;

                        void RemoveBadEffectsAction(bool health)
                        {
                            target.MapInstance.GetBattleEntitiesInRange(new MapCell { X = target.PositionX, Y = target.PositionY }, skill.TargetRange)
                            .Where(e => e.MapMonster == null && e.Hp > 0 && !target.CanAttackEntity(e)).ToList().ForEach(b =>
                            {
                                if (ServerManager.RandomNumber(0, 100) < 25)
                                {
                                    b.DisableBuffs(BuffType.Bad, SecondData + 1);
                                }
                                if (health)
                                {
                                    int healthAmount = casterLevel * 2;
                                    if (b.Hp + healthAmount > b.HpMax)
                                    {
                                        healthAmount = b.HpMax - b.Hp;
                                    }
                                    if (healthAmount > 0)
                                    {
                                        b.Hp += healthAmount;
                                        b.MapInstance.Broadcast(b.GenerateRc(healthAmount));
                                        b.Character?.Session.SendPacket(b.Character.GenerateStat());
                                    }
                                }
                                b.MapInstance.Broadcast(StaticPacketHelper.GenerateEff(b.UserType, b.MapEntityId, 4354));
                            });
                        }

                    }
                }
            }
        }
    }
}