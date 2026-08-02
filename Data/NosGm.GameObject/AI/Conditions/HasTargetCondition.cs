using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;

namespace NosGm.GameObject.AI.Conditions
{
    public class HasTargetCondition : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = entity?.Target ?? blackboard.Get<BattleEntity>("Target");

            if (target != null && target.Hp > 0)
            {
                // Sync to blackboard so child nodes use the updated target
                blackboard.Set("Target", target);
                return BehaviorStatus.Success;
            }
            
            blackboard.Remove("Target");
            if (entity != null) entity.Target = null;
            return BehaviorStatus.Failure;
        }
    }
}
