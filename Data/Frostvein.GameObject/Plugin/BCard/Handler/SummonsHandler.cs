using Game.Configuration.BCards;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SummonsHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Summons;

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

            if (caster.MapMonster?.MonsterVNum == 154)
            {
                return;
            }
            bool move = SecondData != 1382;
            List<MonsterToSummon> summonParameters = new List<MonsterToSummon>();
            int amountToSpawn = firstData;

            if (caster.HasBuff(703) && SecondData == 974)
            {
                amountToSpawn += 2;
                caster.RemoveBuff(703);
            }

            int aliveTime = ServerManager.GetNpcMonster((short)SecondData).RespawnTime / (ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 2400 ? ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 150 ? 1 : 10 : 40) * (ServerManager.GetNpcMonster((short)SecondData).RespawnTime >= 150 ? 4 : 1);
            for (int i = 0; i < amountToSpawn; i++)
            {
                x = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionX);
                y = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionY);
                if (skill != null && caster.Character == null)
                {
                    MapCell randomCell = caster.MapInstance.Map.GetRandomPositionByDistance(caster.PositionX, caster.PositionY, skill.Range, true);
                    if (randomCell != null)
                    {
                        x = randomCell.X;
                        y = randomCell.Y;
                    }
                }
                summonParameters.Add(new MonsterToSummon((short)SecondData, new MapCell { X = x, Y = y }, null,
                    move, false, false, true, false, owner: caster, 10, 0, 15, 0, 0, 0));
            }
            if (ServerManager.RandomNumber() <= Math.Abs(ThirdData) || ThirdData == 0 || ThirdData < 0)
            {
                switch (SubType)
                {
                    case 21:
                        if (CardId == null && SkillVNum == null)
                        {
                            if (caster.MapMonster != null)
                            {
                                caster.BCardDisposables[BCardId]?.Dispose();
                                IDisposable bcardDisposable = null;
                                bcardDisposable = Observable
                                   .Interval(TimeSpan.FromSeconds(5))
                                   .Subscribe(s =>
                                   {
                                       if (caster.BCardDisposables[BCardId] != bcardDisposable)
                                       {
                                           bcardDisposable.Dispose();
                                           return;
                                       }
                                       summonParameters = new List<MonsterToSummon>();
                                       for (int i = 0; i < amountToSpawn; i++)
                                       {
                                           x = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionX);
                                           y = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionY);
                                           summonParameters.Add(new((short)SecondData, new MapCell { X = x, Y = y }, null, move, aliveTime: aliveTime, owner: caster));
                                       }
                                       if (caster.MapMonster.Target != null && caster.MapInstance.GetCharactersInRange(caster.PositionX, caster.PositionY, 5).Any(c => c.BattleEntity.MapEntityId == caster.MapMonster.Target.MapEntityId))
                                       {
                                           EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters));
                                       }
                                   });
                                caster.BCardDisposables[BCardId] = bcardDisposable;
                            }
                        }
                        else
                        {
                            EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters));
                        }
                        break;

                    case 31:
                        summonParameters = new List<MonsterToSummon>();
                        for (int i = 0; i < amountToSpawn; i++)
                        {
                            x = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionX);
                            y = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionY);
                            summonParameters.Add(new((short)SecondData, new MapCell { X = x, Y = y }, null, move, aliveTimeMp: aliveTime, owner: caster));
                        }

                        EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters));
                        break;

                    default:
                        if (!caster.OnDeathEvents.ToList().Any(s => s.EventActionType == EventActionType.SPAWNMONSTERS))
                        {
                            caster.OnDeathEvents.Add(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters));
                        }
                        break;
                }
            }
        }
    }
}
