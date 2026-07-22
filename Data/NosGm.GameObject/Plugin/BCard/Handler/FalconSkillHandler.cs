using Game.Configuration.BCards;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class FalconSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.FalconSkill;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var skill = evnt.Skill;
            var BCardId = evnt.BCard.BCardId;
            var SubType = evnt.BCard.SubType;
            var card = evnt.Card;

            if (SubType.Equals((byte)AdditionalTypes.FalconSkill.Hide) || 
                SubType.Equals((byte)((byte)AdditionalTypes.FalconSkill.Ambush)))
            {
                if (target.Character is Character chara)
                {
                    if (chara.MapInstance.MapInstanceType != MapInstanceType.NormalInstance || chara.MapInstance.Map.MapId != 2004)
                    {
                        if (x != 0 || y != 0)
                        {
                            chara.PositionX = x;
                            chara.PositionY = y;
                            chara.Session.CurrentMapInstance.Broadcast(chara.GenerateTp());
                        }
                        chara.Invisible = true;
                        chara.Mates.Where(s => s.IsTeamMember).ToList().ForEach(s => chara.Session.CurrentMapInstance?.Broadcast(s.GenerateOut()));
                        chara.Session.CurrentMapInstance?.Broadcast(chara.GenerateInvisible());
                    }
                    else if (card != null)
                    {
                        chara.RemoveBuff(card.CardId);
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.FalconSkill.CausingChanceLocation))
            {
                if (target.Character is Character chara)
                {
                    if (chara.Session.CurrentMapInstance != null)
                    {
                        ConcurrentBag<MapMonster> ownedMonsters = new ConcurrentBag<MapMonster>();
                        ServerManager.GetAllMapInstances().ForEach(s => s.Monsters.Where(m => m.Owner?.MapEntityId == chara.CharacterId).ToList().ForEach(mon => ownedMonsters.Add(mon)));
                        if (ownedMonsters.Count >= 3)
                        {
                            chara.BattleEntity.RemoveOwnedMonsters(true);
                        }
                        MapMonster monster = new MapMonster
                        {
                            MonsterVNum = (short)SecondData,
                            MapX = x > 0 ? x : chara.Session.Character.PositionX,
                            MapY = y > 0 ? y : chara.Session.Character.PositionY,
                            MapId = chara.Session.Character.MapInstance.Map.MapId,
                            Position = chara.Session.Character.Direction,
                            MapMonsterId = chara.Session.CurrentMapInstance.GetNextMonsterId(),
                            ShouldRespawn = false,
                            Invisible = true,
                            AliveTime = SecondData == 1436 ? 20 : ServerManager.GetNpcMonster((short)SecondData).BasicCooldown > 0 ? ServerManager.GetNpcMonster((short)SecondData).BasicCooldown : ServerManager.GetNpcMonster((short)SecondData).RespawnTime,
                            Owner = chara.BattleEntity
                        };
                        monster.Initialize(chara.Session.CurrentMapInstance);
                        chara.Session.CurrentMapInstance.AddMonster(monster);
                        chara.Session.CurrentMapInstance.Broadcast(monster.GenerateIn());

                        if (monster.MonsterVNum == 1438 || monster.MonsterVNum == 1439)
                        {
                            monster.Target = monster.BattleEntity;
                            monster.StartLife();
                            NpcMonsterSkill npcMonsterSkill = monster.Skills.FirstOrDefault();
                            target.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(UserType.Monster, monster.MapMonsterId, (byte)UserType.Monster, monster.MapMonsterId,
                                    npcMonsterSkill.SkillVNum, npcMonsterSkill.Skill.Cooldown,
                                    npcMonsterSkill.Skill.AttackAnimation, npcMonsterSkill.Skill.Effect, monster.MapX, monster.MapY,
                                    monster.CurrentHp > 0,
                                    (int)(monster.CurrentHp / monster.MaxHp * 100), 0,
                                    0, 0));
                        }
                    }
                }
            }
            else if (SubType.Equals((byte)AdditionalTypes.FalconSkill.FalconFollowing))
            {
                if (caster.Character != null)
                {
                    caster.Character.BattleEntity.ClearOwnFalcon();
                    caster.Character.BattleEntity.FalconFocusedEntityId = target.MapEntityId;
                    caster.MapInstance.Broadcast($"eff_ob  {(byte)target.UserType} {target.MapEntityId} 1 4269");
                    if (skill != null)
                    {
                        target.MapInstance.Broadcast(StaticPacketHelper.SkillUsed(caster.UserType, caster.MapEntityId, (byte)target.UserType, target.MapEntityId, skill.SkillVNum, skill.Cooldown, skill.AttackAnimation, skill.Effect, x, y, true, target.Hp * 100 / target.HpMax, 0, -1, 0));
                    }
                    caster.Character.BattleEntity.BCardDisposables[BCardId]?.Dispose();
                    IDisposable bcardDisposable = null;
                    bcardDisposable = Observable.Timer(TimeSpan.FromSeconds(60)).Subscribe(s =>
                    {
                        if (caster.Character.BattleEntity.BCardDisposables[BCardId] != bcardDisposable)
                        {
                            bcardDisposable.Dispose();
                            return;
                        }
                        caster.Character.BattleEntity.ClearOwnFalcon();
                    });
                    caster.Character.BattleEntity.BCardDisposables[BCardId] = bcardDisposable;
                }
            }
        }
    }
}