using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Map;
using System.Linq;

namespace NosGm.GameObject.AI.Conditions
{
    public class OwnerIsTargetedCondition : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var mate = blackboard.Get<Mate>("Self");
            if (mate == null || mate.Owner == null) return BehaviorStatus.Failure;

            // Check if owner is targeted by any monsters
            var target = mate.Owner.BattleEntity.TargettedByMonstersList(false).FirstOrDefault();
            
            if (target != null && target.IsAlive)
            {
                // Set pet's target to the monster attacking the owner
                blackboard.Set("Target", target.BattleEntity);
                return BehaviorStatus.Success;
            }

            return BehaviorStatus.Failure;
        }
    }
}
