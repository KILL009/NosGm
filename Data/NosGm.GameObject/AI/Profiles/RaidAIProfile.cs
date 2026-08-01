using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;
using System.Collections.Generic;

namespace NosGm.GameObject.AI.Profiles
{
    public class RaidAIProfile
    {
        public BehaviorTree Tree { get; }

        public RaidAIProfile(MapMonster mob)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", mob);

            // Raid AI Logic:
            // 1. Am I out of my zone (Anti-kite)? If YES -> ReturnToSpawn (Returns Success when healed/reached)
            // 2. Am I in my zone? 
            //    2a. Do I have a target? -> Alert Allies -> Is target in range? -> Attack
            //    2b. If not in range -> Move To Target
            //    2c. No target -> Acquire Target -> Idle

            var returnToSpawnSequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new global::NosGm.AI.Decorators.InverterNode(new IsMonsterInZoneCondition(15)), // If NOT in zone (distance > 15)
                new ReturnToSpawnNode()
            });

            var attackSequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new AlertAlliesNode(10), // Radius 10
                new IsTargetInRangeCondition(mob.Monster.BasicRange),
                new AttackTargetNode()
            });

            var combatSelector = new global::NosGm.AI.Composites.SelectorNode(new List<IBehaviorNode>
            {
                attackSequence,
                new MoveToTargetNode()
            });

            var engageSequence = new global::NosGm.AI.Composites.SequenceNode(new List<IBehaviorNode>
            {
                new AcquireTargetNode(),
                combatSelector
            });

            var rootSelector = new global::NosGm.AI.Composites.SelectorNode(new List<IBehaviorNode>
            {
                returnToSpawnSequence,
                engageSequence
            });

            Tree = new BehaviorTree(rootSelector, blackboard);
        }

        public void Tick() => Tree.Tick();
    }
}
