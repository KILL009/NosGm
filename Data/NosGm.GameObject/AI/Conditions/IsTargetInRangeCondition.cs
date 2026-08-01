using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;

namespace NosGm.GameObject.AI.Conditions
{
    public class IsTargetInRangeCondition : IBehaviorNode
    {
        private readonly int _range;

        public IsTargetInRangeCondition(int range)
        {
            _range = range;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null) return BehaviorStatus.Failure;

            int dist = MapHelper.GetDistance(
                new MapHelper.Location { X = entity.MapX, Y = entity.MapY },
                new MapHelper.Location { X = target.PositionX, Y = target.PositionY }
            );

            return dist <= _range ? BehaviorStatus.Success : BehaviorStatus.Failure;
        }
    }
}
