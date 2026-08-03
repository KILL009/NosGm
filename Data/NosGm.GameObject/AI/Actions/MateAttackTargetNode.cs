using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject.AI.Actions
{
    public class MateAttackTargetNode : IBehaviorNode
    {
        // Keep the pet responsive after either a normal hit or a special skill.
        // Skill cooldown controls when that special may be selected again; it must
        // never block the pet's independent basic-attack loop.
        private const int BasicAttackRecoveryMilliseconds = 1500;

        private DateTime? _actionStartTime;
        private int _actionDurationMilliseconds;

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var mate = blackboard.Get<Mate>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (mate?.BattleEntity == null ||
                mate.Monster == null ||
                target == null ||
                target.Hp <= 0 ||
                mate.BattleEntity.Hp <= 0 ||
                target.MapInstance != mate.BattleEntity.MapInstance)
            {
                ResetAction();
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            if (_actionStartTime.HasValue)
            {
                if ((DateTime.Now - _actionStartTime.Value).TotalMilliseconds <
                    _actionDurationMilliseconds)
                {
                    return BehaviorStatus.Running;
                }

                ResetAction();
                return BehaviorStatus.Success;
            }

            if (!mate.BattleEntity.CanAttackEntity(target))
            {
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            var selectedSkill = SelectReadySpecialSkill(mate, target);
            var isBasicAttack = selectedSkill == null;

            if (isBasicAttack)
            {
                // A basic attack is represented by a null NpcMonsterSkill in
                // Mate.TargetHit. This enters the dedicated LastBasicSkillUse path
                // instead of inheriting a special skill's 30+ second cooldown.
                if (!mate.CanUseBasicSkill() ||
                    mate.BattleEntity.GetDistance(target) > mate.Monster.BasicRange + 1)
                {
                    return BehaviorStatus.Success;
                }
            }

            // Do not start an action lock until every validation has passed. Failed
            // MP, range or cooldown checks must not freeze the behavior tree.
            if (target.MapMonster != null)
            {
                target.MapMonster.AddToAggroList(mate.BattleEntity);
                target.MapMonster.Target = mate.BattleEntity;
            }

            mate.TargetHit(target, selectedSkill);

            var castTimeMilliseconds = selectedSkill?.Skill != null &&
                                       selectedSkill.Skill.CastEffect != 0
                ? Math.Max(0, selectedSkill.Skill.CastTime * 100)
                : 0;

            _actionDurationMilliseconds = Math.Max(
                BasicAttackRecoveryMilliseconds,
                castTimeMilliseconds);
            _actionStartTime = DateTime.Now;
            return BehaviorStatus.Running;
        }

        private static NpcMonsterSkill SelectReadySpecialSkill(Mate mate, BattleEntity target)
        {
            IEnumerable<NpcMonsterSkill> mateSkills = mate.PSkills ??
                                                      Enumerable.Empty<NpcMonsterSkill>();

            var readySkills = mateSkills
                .Where(skill => skill?.Skill != null)
                // The normal attack is handled by Mate.TargetHit(target, null).
                .Where(skill => skill.SkillVNum != mate.Monster.BasicSkill)
                .Where(skill => skill.Rate > 0)
                .Where(skill => skill.CanBeUsed())
                .Where(skill => mate.BattleEntity.Mp >= skill.Skill.MpCost)
                .Where(skill => skill.Skill.TargetType != 0 ||
                                mate.BattleEntity.GetDistance(target) <= skill.Skill.Range)
                .OrderBy(_ => ServerManager.RandomNumber())
                .ToList();

            foreach (var skill in readySkills)
            {
                if (ServerManager.RandomNumber() < skill.Rate)
                {
                    return skill;
                }
            }

            return null;
        }

        private void ResetAction()
        {
            _actionStartTime = null;
            _actionDurationMilliseconds = 0;
        }
    }
}
