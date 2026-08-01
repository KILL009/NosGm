using NosGm.AI.Core;

namespace NosGm.AI.Decorators
{
    public class RepeatNode : IBehaviorNode
    {
        private readonly IBehaviorNode _child;
        private readonly int _times;
        private int _current;

        public RepeatNode(IBehaviorNode child, int times = -1)
        {
            _child = child;
            _times = times;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            if (_times > 0 && _current >= _times) return BehaviorStatus.Success;

            var status = _child.Tick(blackboard);
            if (status == BehaviorStatus.Success)
            {
                _current++;
                if (_times > 0 && _current >= _times) return BehaviorStatus.Success;
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Failure)
            {
                _current = 0;
                return BehaviorStatus.Failure;
            }
            return status;
        }
    }
}
