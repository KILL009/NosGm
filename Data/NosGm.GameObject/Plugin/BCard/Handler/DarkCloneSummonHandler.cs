using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class DarkCloneSummonHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.DarkCloneSummon;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;

            if (SubType == (byte)AdditionalTypes.DarkCloneSummon.SummonDarkCloneChance)
            {
                if (ServerManager.RandomNumber() < FirstData)
                {
                    List<MonsterToSummon> monstersToSummon = new List<MonsterToSummon>();

                    List<short> monsterVNums = Enumerable.Range(2112, SecondData).OrderBy(t => ServerManager.RandomNumber()).Select(t => (short)t).ToList();

                    for (int i = 0; i < ServerManager.RandomNumber(1, monsterVNums.Count + 1); i++)
                    {
                        short mapX = (short)(caster.PositionX + ServerManager.RandomNumber(-2, 2));
                        short mapY = (short)(caster.PositionY + ServerManager.RandomNumber(-2, 2));

                        if (caster.MapInstance.Map.IsBlockedZone(mapX, mapY))
                        {
                            mapX = caster.PositionX;
                            mapY = caster.PositionY;
                        }

                        monstersToSummon.Add(new MonsterToSummon(monsterVNums[i], new MapCell { X = mapX, Y = mapY }, target,
                            true, false, false, false, false, caster, 5, 0, 0, 1 /* second(s) */, 0, 0));
                    }

                    EventHelper.Instance.RunEvent(new EventContainer(caster.MapInstance, EventActionType.SPAWNMONSTERS, monstersToSummon));
                }
            }
        }
    }
}
