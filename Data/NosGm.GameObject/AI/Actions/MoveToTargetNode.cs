using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;

namespace NosGm.GameObject.AI.Actions
{
    public class MoveToTargetNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null) return BehaviorStatus.Failure;

            int dist = Map.GetDistance(entity.GetPos(), target.GetPos());

            if (dist <= entity.Monster.BasicRange) return BehaviorStatus.Success; // Reached target

            // Use the existing MoveTest logic for step generation
            entity.Target = target;
            entity.MoveTest();
            
            return BehaviorStatus.Running; // Still moving
        }
    }
}
