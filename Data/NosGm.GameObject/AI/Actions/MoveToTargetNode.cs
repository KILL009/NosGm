using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.PathFinder;

namespace NosGm.GameObject.AI.Actions
{
    public class MoveToTargetNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null) return BehaviorStatus.Failure;

            // Simplified BT wrapper for movement. 
            // In a full implementation, we run AStarPathFinder.FindPath here and move step by step.
            
            // Distance check
            int dist = MapHelper.GetDistance(
                new MapHelper.Location { X = entity.MapX, Y = entity.MapY },
                new MapHelper.Location { X = target.PositionX, Y = target.PositionY }
            );

            if (dist <= entity.Monster.AttackRange) return BehaviorStatus.Success; // Reached target

            // Move
            entity.MoveTo(target.PositionX, target.PositionY);
            
            return BehaviorStatus.Running; // Still moving
        }
    }
}
