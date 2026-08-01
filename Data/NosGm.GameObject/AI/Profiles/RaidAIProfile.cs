using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;
using System.Collections.Generic;

namespace NosGm.GameObject.AI.Profiles
{
    public class RaidAIProfile : IAIProfile
    {
        public BehaviorTree Tree { get; }

        public RaidAIProfile(MapMonster mob)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", mob);

            var returnToSpawnSequence = new global::NosGm.AI.Composites.SequenceNode(
                new global::NosGm.AI.Decorators.InverterNode(new IsMonsterInZoneCondition(15)), 
                new ReturnToSpawnNode()
            );

            var attackSequence = new global::NosGm.AI.Composites.SequenceNode(
                new AlertAlliesNode(10),
                new IsTargetInRangeCondition(mob.Monster.BasicRange),
                new AttackTargetNode()
            );

            var combatSelector = new global::NosGm.AI.Composites.SelectorNode(
                attackSequence,
                new MoveToTargetNode()
            );

            var engageSequence = new global::NosGm.AI.Composites.SequenceNode(
                new FindTargetNode(),
                combatSelector
            );

            var rootSelector = new global::NosGm.AI.Composites.SelectorNode(
                returnToSpawnSequence,
                engageSequence
            );

            Tree = new BehaviorTree(rootSelector, blackboard);
        }

        public void Tick() => Tree.Tick();
    }
}
