using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;

namespace NosGm.GameObject.AI.Conditions
{
    public class HasTargetCondition : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var target = blackboard.Get<BattleEntity>("Target");
            if (target != null && target.Character != null && target.Character.Hp > 0)
            {
                return BehaviorStatus.Success;
            }
            
            blackboard.Remove("Target");
            return BehaviorStatus.Failure;
        }
    }
}
