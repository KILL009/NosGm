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
    public class MeteoriteTeleportHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.MeteoriteTeleport;

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

            if (SubType == (byte)AdditionalTypes.MeteoriteTeleport.SummonInVisualRange)
            {
                MapInstance mapInstance = target?.MapInstance;

                if (mapInstance != null)
                {
                    List<MonsterToSummon> monstersToSummon = new List<MonsterToSummon>();

                    for (int i = 0; i < 73; i++)
                    {
                        monstersToSummon.Add(new(2328, new MapCell { X = 0, Y = 0 }, null, false, hasDelay: (short)ServerManager.RandomNumber(0, 5)));
                    }

                    EventHelper.Instance.RunEvent(new EventContainer(mapInstance, EventActionType.SPAWNMONSTERS, monstersToSummon));
                }
            }
            else if (SubType == (byte)AdditionalTypes.MeteoriteTeleport.TransformTarget)
            {
                int[] morphVNums = new int[] { 1000099, 1000156 };

                if (caster != null
                    && target?.Character?.MapInstance != null
                    && !morphVNums.Contains(target.Character.Morph)
                    && ServerManager.RandomNumber() < 50)
                {
                    target.Character.MapInstance.Broadcast(target.Character.GenerateEff(4054));
                    target.Character.IsMorphed = true;
                    target.Character.UseSp = false;
                    target.Character.PreviousMorph = target.Character.Morph;
                    target.Character.Morph = morphVNums.OrderBy(rnd => ServerManager.RandomNumber()).First();

                    switch (target.Character.Morph)
                    {
                        case 1000099: // Hamster
                            target.AddBuff(new Buff(478, caster.Level, false), caster);
                            break;

                        case 1000156: // Bushtail
                            target.AddBuff(new Buff(477, caster.Level, true), caster);
                            break;
                    }

                    target.Character.Session.SendPacket(target.Character.GenerateSki());
                    target.Character.Session.SendPackets(target.Character.GenerateQuicklist());
                    target.Character.MapInstance.Broadcast(target.Character.GenerateCMode());
                }
            }
            else if (SubType == (byte)AdditionalTypes.MeteoriteTeleport.TeleportForward)
            {
                target.TeleportTo(target.MapInstance.Map.GetRandomPositionByDistance(target.PositionX, target.PositionY, (short)FirstData));
            }
            else if (SubType == (byte)AdditionalTypes.MeteoriteTeleport.CauseMeteoriteFall)
            {
                MapInstance mapInstance = target?.MapInstance;

                if (mapInstance != null)
                {
                    List<MonsterToSummon> monstersToSummon = new List<MonsterToSummon>();

                    int range = 10;

                    for (int i = 0; i < 10 + (int)(target.Level / (double)FirstData); i++)
                    {
                        MapCell mapCell = new MapCell
                        {
                            X = (short)(target.PositionX + ServerManager.RandomNumber(-range, range + 1)),
                            Y = (short)(target.PositionY + ServerManager.RandomNumber(-range, range + 1)),
                        };

                        monstersToSummon.Add(new((short)ServerManager.RandomNumber(2352, 2353 + 1), mapCell, null, false, owner: target, hasDelay: ServerManager.RandomNumber<short>(0, 10 + 1))
                        {
                            IsMeteorite = true,
                        });
                    }

                    if (monstersToSummon.Any())
                    {
                        EventHelper.Instance.RunEvent(new EventContainer(mapInstance, EventActionType.SPAWNMONSTERS, monstersToSummon));
                    }
                }
            }
            else if (SubType == (byte)AdditionalTypes.MeteoriteTeleport.TeleportYouAndGroupToSavedLocation)
            {
                if (target.Character is Character character && character.CharacterId == caster?.Character?.CharacterId)
                {
                    if (character.SavedLocation == null)
                    {
                        character.SavedLocation = new MapCell
                        {
                            X = character.PositionX,
                            Y = character.PositionY
                        };

                        target.MapInstance?.Broadcast($"eff_g 4497 {character.CharacterId} {character.SavedLocation.X} {character.SavedLocation.Y} 0");
                    }
                    else
                    {
                        MapCell mapCellTo = new MapCell
                        {
                            X = character.SavedLocation.X,
                            Y = character.SavedLocation.Y,
                        };

                        MapCell mapCellFrom = new MapCell
                        {
                            X = target.PositionX,
                            Y = target.PositionY
                        };

                        target.MapInstance?.Broadcast($"eff_g 4483 {character.CharacterId} {mapCellFrom.X} {mapCellFrom.Y} 0");

                        Observable.Timer(TimeSpan.FromSeconds(1))
                            .Subscribe(t =>
                            {
                                target.TeleportTo(mapCellTo);

                                target.MapInstance?.Broadcast($"eff_g 4497 {character.CharacterId} {mapCellTo.X} {mapCellTo.Y} 1");

                                List<Character> friendCharacters = new List<Character>();

                                if (character.Family?.FamilyCharacters != null)
                                {
                                    friendCharacters.AddRange(character.Family.FamilyCharacters.Select(fc => ServerManager.Instance.GetCharacterById(fc.CharacterId)).Where(c => c != null));
                                }

                                if (character.Group?.Sessions != null)
                                {
                                    friendCharacters.AddRange(character.Group.Sessions.Where(s => s.Character != null).Select(s => s.Character));
                                }

                                friendCharacters.Where(c => c.CharacterId != character.CharacterId && c.MapInstanceId == character.MapInstanceId
                                    && Map.GetDistance(c.BattleEntity.GetPos(), mapCellFrom) <= skill.TargetRange).OrderBy(c => Map.GetDistance(c.BattleEntity.GetPos(), mapCellFrom)).Take(FirstData).ToList()
                                .ForEach(c => c.BattleEntity.TeleportTo(mapCellTo, 3));
                            });
                    }
                }
            }

        }
    }
}
