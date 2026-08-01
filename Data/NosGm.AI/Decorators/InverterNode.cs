using NosGm.AI.Core;

namespace NosGm.AI.Decorators
{
    public class InverterNode : IBehaviorNode
    {
        private readonly IBehaviorNode _child;

        public InverterNode(IBehaviorNode child)
        {
            _child = child;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var status = _child.Tick(blackboard);
            if (status == BehaviorStatus.Success) return BehaviorStatus.Failure;
            if (status == BehaviorStatus.Failure) return BehaviorStatus.Success;
            return status;
        }
    }
}
