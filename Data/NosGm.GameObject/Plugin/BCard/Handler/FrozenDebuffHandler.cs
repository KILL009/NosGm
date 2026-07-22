using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class FrozenDebuffHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.FrozenDebuff;

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

            if (SubType == (byte)AdditionalTypes.FrozenDebuff.GlacerusSkill)
            {
                MapInstance mapInstance = caster.MapInstance;

                if (mapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    mapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GLACERUS_GRRR"), 0));

                    // SafeZone
                    for (short monsterVNum = 4280; monsterVNum <= 4282; monsterVNum++)
                    {
                        EventHelper.Instance.RunEvent(new EventContainer(mapInstance, EventActionType.SPAWNMONSTER, new MonsterToSummon(2018, new MapCell { X = 0, Y = 0 }, null, false, isHostile: false)
                        {
                            AfterSpawnEvents = new List<EventContainer>()
                                            {
                                                new EventContainer(mapInstance, EventActionType.EFFECT, new Tuple<short, int>(monsterVNum, 0)),
                                                new EventContainer(mapInstance, EventActionType.REMOVEAFTER, 15)
                                            }
                        }));
                    }

                    Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(t =>
                    {
                        foreach (Character character in mapInstance.Sessions.Where(s => s?.Character != null).Select(s => s.Character))
                        {
                            // Wind

                            character.Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, character.CharacterId, 4293));

                            // Freeze

                            if (character.Hp < 1
                                || character.HasBuff(BCardType.CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce))
                            {
                                continue;
                            }

                            IEnumerable<MapMonster> safeZoneList = mapInstance.GetMonsterInRangeList(character.PositionX, character.PositionY, 5)
                                .Where(m => m.MonsterVNum >= 2018 && m.MonsterVNum <= 2018);

                            if (!safeZoneList.Any())
                            {
                                character.AddBuff(new Buff(569, caster.Level), caster);

                                if (!mapInstance.Sessions.Any(s => s.Character != null
                                    && !s.Character.HasBuff(BCardType.CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce)))
                                {
                                    EventHelper.Instance.RunEvent(new EventContainer(mapInstance, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg("Glacerus the frosty prepares to launch the blizzard.", 0)));
                                    Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => EventHelper.Instance.RunEvent(new EventContainer(mapInstance, EventActionType.SCRIPTEND, (byte)4)));
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}
