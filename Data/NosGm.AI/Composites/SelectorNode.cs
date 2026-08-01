using NosGm.AI.Core;

namespace NosGm.AI.Composites
{
    public class SelectorNode : IBehaviorNode
    {
        private readonly IBehaviorNode[] _children;
        private int _currentIndex;

        public SelectorNode(params IBehaviorNode[] children)
        {
            _children = children;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            while (_currentIndex < _children.Length)
            {
                var status = _children[_currentIndex].Tick(blackboard);

                if (status != BehaviorStatus.Failure)
                {
                    if (status == BehaviorStatus.Success)
                        _currentIndex = 0;
                    return status;
                }
                
                _currentIndex++;
            }

            _currentIndex = 0;
            return BehaviorStatus.Failure;
        }
    }
}
