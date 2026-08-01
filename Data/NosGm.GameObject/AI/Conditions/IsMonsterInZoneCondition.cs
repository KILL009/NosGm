using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Map;
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
            var distance = Map.Map.GetDistance(entity.GetPos(), spawnPos);

            if (distance <= _maxDistance)
            {
                return BehaviorStatus.Success;
            }

            return BehaviorStatus.Failure;
        }
    }
}
