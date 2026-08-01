using NosGm.AI.Core;

namespace NosGm.AI.Composites
{
    public class SequenceNode : IBehaviorNode
    {
        private readonly IBehaviorNode[] _children;
        private int _currentIndex;

        public SequenceNode(params IBehaviorNode[] children)
        {
            _children = children;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            while (_currentIndex < _children.Length)
            {
                var status = _children[_currentIndex].Tick(blackboard);

                if (status != BehaviorStatus.Success)
                {
                    if (status == BehaviorStatus.Failure)
                        _currentIndex = 0; 
                    return status;
                }
                
                _currentIndex++;
            }

            _currentIndex = 0;
            return BehaviorStatus.Success;
        }
    }
}
