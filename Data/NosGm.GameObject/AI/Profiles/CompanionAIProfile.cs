using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;

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
            Tree = new BehaviorTree(new global::NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
