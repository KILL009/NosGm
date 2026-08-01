namespace NosGm.AI.Core
{
    public class BehaviorTree
    {
        public IBehaviorNode Root { get; }
        public Blackboard Blackboard { get; }

        public BehaviorTree(IBehaviorNode root, Blackboard blackboard = null)
        {
            Root = root;
            Blackboard = blackboard ?? new Blackboard();
        }

        public BehaviorStatus Tick()
        {
            return Root.Tick(Blackboard);
        }
    }
}
