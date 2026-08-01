using NosGm.AI.Core;
using NosGm.GameObject;

namespace NosGm.GameObject.AI.Conditions
{
    public class IsHealthBelowCondition : IBehaviorNode
    {
        private readonly double _percentage;

        public IsHealthBelowCondition(double percentage)
        {
            _percentage = percentage;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            if (entity == null) return BehaviorStatus.Failure;

            double currentHpPct = (double)entity.BattleEntity.Hp / entity.BattleEntity.HpMax;
            return currentHpPct <= _percentage ? BehaviorStatus.Success : BehaviorStatus.Failure;
        }
    }
}
