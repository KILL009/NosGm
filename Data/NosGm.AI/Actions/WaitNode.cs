using NosGm.AI.Core;
using System;

namespace NosGm.AI.Actions
{
    public class WaitNode : IBehaviorNode
    {
        private readonly Func<Blackboard, TimeSpan> _delayFunc;
        private DateTime? _startTime;

        public WaitNode(TimeSpan staticDelay)
        {
            _delayFunc = _ => staticDelay;
        }

        public WaitNode(Func<Blackboard, TimeSpan> delayFunc)
        {
            _delayFunc = delayFunc;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            if (_startTime == null)
            {
                _startTime = DateTime.Now;
            }

            var delay = _delayFunc(blackboard);

            if (DateTime.Now - _startTime.Value >= delay)
            {
                _startTime = null; // reset for future ticks
                return BehaviorStatus.Success;
            }

            return BehaviorStatus.Running;
        }
    }
}
