using Game.Configuration.BCards;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class SecondSPCardHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.SecondSPCard;

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
            List<MonsterToSummon> summonParameters2 = new List<MonsterToSummon>();
            if (target.Character != null)
            {
                aliveTime = ServerManager.GetNpcMonster((short)SecondData).RespawnTime / (ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 2400 ? ServerManager.GetNpcMonster((short)SecondData).RespawnTime < 150 ? 1 : 10 : 40) * (ServerManager.GetNpcMonster((short)SecondData).RespawnTime >= 150 ? 4 : 1);
                for (int i = 0; i < firstData; i++)
                {
                    x = SecondData == 945 ? target.PositionX : (short)(ServerManager.RandomNumber(-1, 1) + target.PositionX);
                    y = SecondData == 945 ? target.PositionY : (short)(ServerManager.RandomNumber(-1, 1) + target.PositionY);
                    if (target.MapInstance.Map.IsBlockedZone(x, y))
                    {
                        x = target.PositionX;
                        y = target.PositionY;
                    }
                    summonParameters2.Add(new((short)SecondData, new MapCell { X = x, Y = y }, null, true, isHostile: SecondData != 945, owner: target, aliveTime: aliveTime));
                }
                if (caster?.Character?.Group?.Raid?.Id == 8 && caster?.Character?.Session != null)
                {
                    caster.Character.Session.SendPacket(caster?.Character?.GenerateSay(Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                    return;
                }
                if (ServerManager.RandomNumber() <= Math.Abs(ThirdData) || ThirdData == 0 || ThirdData < 0)
                {
                    switch (SubType)
                    {
                        case ((byte)AdditionalTypes.SecondSPCard.PlantBomb):
                            {
                                if (target.MapInstance.Monsters.Any(s => s.Owner != null && s.Owner.MapEntityId == target.MapEntityId && s.MonsterVNum == SecondData))
                                {
                                    //Attack
                                    target.MapInstance.Monsters.Where(s => s.Owner?.MapEntityId == target.MapEntityId && s.MonsterVNum == SecondData).ToList().ForEach(s =>
                                    {
                                        s.Target = s.BattleEntity;
                                    });

                                    short NewCooldown = 300;
                                    Observable.Timer(TimeSpan.FromMilliseconds(skill.Cooldown * 100 + 50)).Subscribe(s =>
                                    {
                                        target.Character.Session.CurrentMapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Player,
                                            target.Character.CharacterId, 1, target.Character.CharacterId, skill.SkillVNum,
                                            NewCooldown, 2, skill.Effect,
                                            target.Character.PositionX, target.Character.PositionY, true,
                                            (int)(target.Character.Hp / target.Character.HPLoad() * 100), 0, -1,
                                            (byte)(skill.SkillType - 1)));
                                    });
                                    DateTime EndCooldownTime = DateTime.Now.AddMilliseconds(NewCooldown * 100);
                                    target.Character.SkillsSp.FirstOrDefault(s => s.SkillVNum == SkillVNum).LastUse = EndCooldownTime;
                                    Observable.Timer(EndCooldownTime).Subscribe(s =>
                                    {
                                        if (target.Character.SkillsSp != null && target.Character.SkillsSp.FirstOrDefault(c => c.SkillVNum == SkillVNum) is CharacterSkill cSkill)
                                        {
                                            if (cSkill.LastUse <= EndCooldownTime)
                                            {
                                                target.Character.Session.SendPacket(StaticPacketHelper.SkillReset(skill.CastId));
                                            }
                                        }
                                    });
                                }
                                else
                                {
                                    EventHelper.Instance.RunEvent(new EventContainer(target.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters2));
                                }
                            }
                            break;

                        case ((byte)AdditionalTypes.SecondSPCard.PlantSelfDestructionBomb):
                            {
                                EventHelper.Instance.RunEvent(new EventContainer(target.MapInstance, EventActionType.SPAWNMONSTERS, summonParameters2));
                            }
                            break;
                    }
                }
                target.Character.Session.SendPacket(target.Character.GenerateStat());
            }
        }
    }
}
