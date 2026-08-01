using NosGm.AI.Core;
using NosGm.GameObject;

namespace NosGm.GameObject.AI.Profiles
{
    public class CompanionAIProfile
    {
        public BehaviorTree Tree { get; }

        public CompanionAIProfile(Mate companion)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", companion);

            // Companions use skills more actively than pets
            Tree = new BehaviorTree(new NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
