using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;
using System.Collections.Generic;

namespace NosGm.GameObject.AI.Profiles
{
    public class BossAIProfile
    {
        public BehaviorTree Tree { get; }

        public BossAIProfile(MapMonster boss)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", boss);

            // Phase 3: Below 30% HP -> Uses Ultimate Skill (Index 2)
            var phase3Sequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new IsHealthBelowCondition(0.30),
                new IsTargetInRangeCondition(boss.Monster.BasicRange + 2), // Example
                new AttackTargetNode(2) 
            });

            // Phase 2: Below 60% HP -> Uses Special Skill (Index 1)
            var phase2Sequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new IsHealthBelowCondition(0.60),
                new IsTargetInRangeCondition(boss.Monster.BasicRange),
                new AttackTargetNode(1)
            });

            // Phase 1: Basic Attack -> Uses Basic Skill (Index 0)
            var phase1Sequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new IsTargetInRangeCondition(boss.Monster.BasicRange),
                new AttackTargetNode(0)
            });

            var attackSelector = new global::NosGm.AI.Composites.SelectorNode(new List<IBehaviorNode>
            {
                phase3Sequence,
                phase2Sequence,
                phase1Sequence
            });

            // Full boss logic: acquire target -> move to target -> select attack based on phase
            var rootSequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new AcquireTargetNode(),
                new global::NosGm.AI.Composites.SelectorNode(new List<IBehaviorNode>
                {
                    attackSelector,
                    new MoveToTargetNode()
                })
            });

            Tree = new BehaviorTree(rootSequence, blackboard);
        }

        public void Tick() => Tree.Tick();
    }
}
