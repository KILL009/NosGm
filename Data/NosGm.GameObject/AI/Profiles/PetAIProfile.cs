using NosGm.AI.Core;
using NosGm.GameObject;

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
            Tree = new BehaviorTree(new NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
