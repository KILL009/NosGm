using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Helpers;

namespace NosGm.GameObject.AI.Actions
{
    public class MateAttackTargetNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            Mate mate = blackboard.Get<Mate>("Self");
            BattleEntity target = blackboard.Get<BattleEntity>("Target");

            if (mate?.BattleEntity == null ||
                mate.Monster == null ||
                target == null ||
                target.Hp <= 0 ||
                mate.BattleEntity.Hp <= 0 ||
                target.MapInstance != mate.BattleEntity.MapInstance)
            {
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            if (!mate.BattleEntity.CanAttackEntity(target))
            {
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            // The behavior tree owns only automatic basic attacks. Pet special
            // skills belong exclusively to the client-driven u_pet handler. Mixing
            // both authorities caused a special skill's 30+ second cooldown to
            // suppress the independent basic-attack loop.
            int allowedBasicRange = mate.Monster.BasicRange <= 0
                ? 1
                : mate.Monster.BasicRange + 1;

            if (!mate.CanUseBasicSkill() ||
                mate.BattleEntity.GetDistance(target) > allowedBasicRange)
            {
                return BehaviorStatus.Success;
            }

            if (target.MapMonster != null)
            {
                target.MapMonster.AddToAggroList(mate.BattleEntity);
                target.MapMonster.Target = mate.BattleEntity;
            }

            long experienceBefore = MateCombatDiagnostics.BeginBasicAttack(
                mate,
                target,
                "AI");

            // A null NpcMonsterSkill deliberately enters Mate.TargetHit's dedicated
            // LastBasicSkillUse path. No special-skill cooldown is consulted here.
            mate.TargetHit(target, null);
            MateCombatDiagnostics.ObserveExperienceAfterAttack(
                mate,
                target,
                experienceBefore);
            return BehaviorStatus.Success;
        }
    }
}
