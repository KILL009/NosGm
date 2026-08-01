using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;

namespace NosGm.GameObject.AI.Profiles
{
    public class PetAIProfile
    {
        public BehaviorTree Tree { get; }

        public PetAIProfile(Mate pet)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", pet);

            // Pets follow the owner and attack owner's target
            // To be implemented: Follow Owner -> Assist Target -> Defend
            Tree = new BehaviorTree(new global::NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
