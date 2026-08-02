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
            var target = blackboard.Get<BattleEntity>("Target");

            if (target != null &&
                target.Hp > 0 &&
                (entity == null || target.MapInstance == entity.MapInstance))
            {
                // Keep the legacy target property synchronized with the behavior-tree
                // blackboard. Pet defence and older combat systems still read it.
                if (entity != null)
                {
                    entity.Target = target;
                }

                return BehaviorStatus.Success;
            }

            if (entity != null)
            {
                entity.Target = null;
            }

            blackboard.Remove("Target");
            return BehaviorStatus.Failure;
        }
    }
}
