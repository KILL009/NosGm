using NosGm.AI.Core;
using NosGm.GameObject;
using System.Linq;

namespace NosGm.GameObject.AI.Conditions
{
    public class OwnerIsTargetedCondition : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var mate = blackboard.Get<Mate>("Self");
            if (mate?.Owner?.BattleEntity == null || mate.BattleEntity == null || !mate.IsAlive)
            {
                return BehaviorStatus.Failure;
            }

            // Include the owner's active mates. This allows one pet to react when the
            // owner or another team mate is being attacked and keeps the behaviour
            // compatible with the legacy Target property synchronized by mob AI.
            var target = mate.Owner.BattleEntity
                .TargettedByMonstersList(true)
                .Where(monster => monster != null &&
                                  monster.Hp > 0 &&
                                  monster.MapInstance == mate.BattleEntity.MapInstance)
                .OrderBy(monster => Map.GetDistance(mate.BattleEntity.GetPos(), monster.GetPos()))
                .FirstOrDefault();

            if (target == null)
            {
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            blackboard.Set("Target", target);
            return BehaviorStatus.Success;
        }
    }
}
