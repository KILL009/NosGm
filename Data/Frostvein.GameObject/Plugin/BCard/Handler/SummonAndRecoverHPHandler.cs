using Game.Configuration.BCards;
using Frostvein.Data;
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
    public class SummonAndRecoverHPHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SummonAndRecoverHP;

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

            int aliveTime = 0;
            List<MonsterToSummon> summonParameters3 = new List<MonsterToSummon>();
            if (ServerManager.RandomNumber() <= Math.Abs(ThirdData) || ThirdData == 0 || ThirdData < 0)
            {
                aliveTime = ServerManager.GetNpcMonster((short)SecondData).RespawnTime / (ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 2400 ? ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 150 ? 1 : 10 : 40) * (ServerManager.GetNpcMonster((short)SecondData).RespawnTime >= 150 ? 4 : 1);
                switch (SubType)
                {
                    case (byte)AdditionalTypes.SummonAndRecoverHP.ChanceSummon:
                        if (caster.GetOwnedMonsters().Where(s => s.MonsterVNum == SecondData).Count() == 0)
                        {
                            summonParameters3.Add(new((short)SecondData, new MapCell { X = caster.PositionX, Y = caster.PositionY }, null, true, aliveTime: aliveTime, owner: caster));
                            EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters3));
                        }
                        break;

                    case (byte)AdditionalTypes.SummonAndRecoverHP.RestoreHP:
                        Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(spawn =>
                        {
                            if (caster.MapMonster != null && caster.MapMonster.Owner != null && caster.MapMonster.Owner.GetOwnedMonsters().Where(s => s.MonsterVNum == caster.MapMonster.MonsterVNum).Count() == 1)
                            {
                                for (int i = 0; i < firstData - 1; i++)
                                {
                                    x = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionX);
                                    y = (short)(ServerManager.RandomNumber(-1, 1) + caster.PositionY);
                                    summonParameters3.Add(new(caster.MapMonster.MonsterVNum, new MapCell { X = x, Y = y }, null, true, aliveTime: aliveTime, owner: caster.MapMonster.Owner));
                                }
                                EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters3));
                            }
                        });
                        break;
                }
            }
        }
    }
}
