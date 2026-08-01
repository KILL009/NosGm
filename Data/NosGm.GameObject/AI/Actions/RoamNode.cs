using NosGm.AI.Core;
using NosGm.GameObject;

namespace NosGm.GameObject.AI.Actions
{
    public class RoamNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");

            if (entity == null) return BehaviorStatus.Failure;

            // Using the existing MoveTest logic for step generation when idle
            entity.MoveTest();
            
            return BehaviorStatus.Success; 
        }
    }
}
