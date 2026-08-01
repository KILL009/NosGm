using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;
using System.Collections.Generic;

namespace NosGm.GameObject.AI.Profiles
{
    public class PetAIProfile : IAIProfile
    {
        public BehaviorTree Tree { get; }

        public PetAIProfile(Mate pet)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", pet);

            // 1. Defend Owner if targeted
            var defendOwnerSequence = new global::NosGm.AI.Composites.SequenceNode(
                new OwnerIsTargetedCondition(),
                new MateAttackTargetNode()
            );

            // 2. Follow Owner (fallback)
            var followOwnerNode = new FollowOwnerNode();

            var rootSelector = new global::NosGm.AI.Composites.SelectorNode(
                defendOwnerSequence,
                followOwnerNode
            );

            Tree = new BehaviorTree(rootSelector, blackboard);
        }

        public void Tick() => Tree.Tick();
    }
}
