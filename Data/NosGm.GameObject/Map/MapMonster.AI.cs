using System;
using System.Linq;
using NosGm.Domain;
using static NosGm.Domain.BCardType;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.AI.Profiles;

namespace NosGm.GameObject
{
    public partial class MapMonster
    {
        public MobAIProfile AiProfile { get; set; }

        public void InitializeAI()
        {
            AiProfile = new MobAIProfile(this);
        }

        public int StartAttackInstantly(BattleEntity target, NpcMonsterSkill npcMonsterSkill)
        {
            if (Monster == null || HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
            {
                return 0;
            }

            var castTime = 0;
            if (npcMonsterSkill != null)
            {
                if (CurrentMp < npcMonsterSkill.Skill.MpCost)
                {
                    return 0;
                }

                _previousSkillVNum = npcMonsterSkill.SkillVNum;
                npcMonsterSkill.LastSkillUse = DateTime.Now;
                DecreaseMp(npcMonsterSkill.Skill.MpCost);
                
                MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Monster, MapMonsterId,
                    target.UserType, target.MapEntityId,
                    npcMonsterSkill.Skill.CastAnimation, npcMonsterSkill.Skill.CastEffect,
                    npcMonsterSkill.Skill.SkillVNum));

                if (npcMonsterSkill.Skill.CastEffect != 0)
                {
                    MapInstance.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Monster, MapMonsterId,
                            npcMonsterSkill.Skill.CastEffect), MapX, MapY);
                    castTime = npcMonsterSkill.Skill.CastTime * 100;
                }
            }

            return castTime;
        }

        public void ExecuteAttackInstantly(BattleEntity target, NpcMonsterSkill npcMonsterSkill)
        {
            if (target.Hp <= 0 || !IsAlive)
            {
                return;
            }

            if (target.Character != null && (target.Character.Invisible || target.Character.InvisibleGm) && !CanSeeHiddenThings)
            {
                return;
            }

            if (npcMonsterSkill != null && npcMonsterSkill.Skill.TargetType == 1 && npcMonsterSkill.Skill.HitType == 1)
            {
                var possibleTargets = MapInstance.BattleEntities
                    .Where(e => e.UserType == UserType.Player && e.Character != null && BattleEntity.CanAttackEntity(e) &&
                                !e.Character.InvisibleGm && (!e.Character.Invisible || CanSeeHiddenThings) &&
                                Map.GetDistance(GetPos(), e.GetPos()) <= npcMonsterSkill.Skill.Range)
                    .ToList();
                
                foreach (var pTarget in possibleTargets)
                {
                    ApplyDamage(pTarget, npcMonsterSkill);
                }
            }
            else
            {
                ApplyDamage(target, npcMonsterSkill);
            }
        }

        private void ApplyDamage(BattleEntity targetEntity, NpcMonsterSkill npcMonsterSkill)
        {
            int hitmode = 0;
            bool onyxWings = false, zephyrWings = false, dragonBuff = false;

            var damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this),
                targetEntity, npcMonsterSkill?.Skill, ref hitmode,
                ref onyxWings, ref zephyrWings, ref dragonBuff);

            if (targetEntity.Character != null && targetEntity.Character.HasGodMode ||
                targetEntity.Mate != null && targetEntity.Mate.Owner.HasGodMode)
            {
                damage = 0;
            }

            TargetHit2(targetEntity, npcMonsterSkill, damage, hitmode);
        }
    }
}
