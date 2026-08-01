using NosGm.AI.Core;
using NosGm.GameObject;
using System.Linq;

namespace NosGm.GameObject.AI.Actions
{
    public class AttackTargetNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null || !target.Character.IsAlive) return BehaviorStatus.Failure;

            // Trigger the existing attack logic in MapMonster (e.g. GenerateAttack)
            // Assuming the monster is already in range (checked by condition node)
            if (entity.IsCastingSkill) return BehaviorStatus.Running;
            
            // Initiate attack
            entity.MonsterRunSkill();
            
            return BehaviorStatus.Success;
        }
    }
}
