using NosGm.AI.Core;
using NosGm.GameObject;

namespace NosGm.GameObject.AI.Conditions
{
    public class HasTargetCondition : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var target = blackboard.Get<BattleEntity>("Target");
            if (target != null && target.Character != null && target.Character.IsAlive)
            {
                return BehaviorStatus.Success;
            }
            
            blackboard.Remove("Target");
            return BehaviorStatus.Failure;
        }
    }
}
