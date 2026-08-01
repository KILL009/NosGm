using NosGm.AI.Core;
using NosGm.GameObject;

using System;

namespace NosGm.GameObject.AI.Conditions
{
    public class IsMonsterInZoneCondition : IBehaviorNode
    {
        private readonly int _maxDistance;

        public IsMonsterInZoneCondition(int maxDistance = 15)
        {
            _maxDistance = maxDistance;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            if (entity == null) return BehaviorStatus.Failure;

            var spawnPos = new MapCell { X = entity.FirstX, Y = entity.FirstY };
            int distance = Map.GetDistance(new MapCell { X = entity.MapX, Y = entity.MapY }, spawnPos);

            if (distance <= _maxDistance)
            {
                return BehaviorStatus.Success;
            }

            return BehaviorStatus.Failure;
        }
    }
}
