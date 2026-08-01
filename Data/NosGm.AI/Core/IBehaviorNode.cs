namespace NosGm.AI.Core
{
    public interface IBehaviorNode
    {
        BehaviorStatus Tick(Blackboard blackboard);
    }
}
