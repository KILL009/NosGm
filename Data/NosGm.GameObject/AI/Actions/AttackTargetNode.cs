using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using System.Linq;
using System;

namespace NosGm.GameObject.AI.Actions
{
    public class AttackTargetNode : IBehaviorNode
    {
        private DateTime? _castStartTime;
        private int _castTimeMs;

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null || target.Hp <= 0) return BehaviorStatus.Failure;

            var skill = entity.Skills?.FirstOrDefault();

            if (_castStartTime == null)
            {
                // First tick: Start cast
                _castTimeMs = entity.StartAttackInstantly(target, skill);
                _castStartTime = DateTime.Now;
                return BehaviorStatus.Running;
            }
            else
            {
                if ((DateTime.Now - _castStartTime.Value).TotalMilliseconds >= _castTimeMs)
                {
                    entity.ExecuteAttackInstantly(target, skill);
                    _castStartTime = null; // reset
                    return BehaviorStatus.Success;
                }
                return BehaviorStatus.Running;
            }
        }
    }
}
