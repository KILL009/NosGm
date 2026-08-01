using NosGm.AI.Core;
using NosGm.AI.Composites;
using NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;

namespace NosGm.GameObject.AI.Profiles
{
    public class MobAIProfile
    {
        public BehaviorTree Tree { get; }

        public MobAIProfile(MapMonster monster)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", monster);

            // BT Definition for a standard Mob
            // Selector:
            // 1. Sequence: HasTarget -> If In Range -> Attack, Else -> Move To Target
            // 2. Sequence: Find Target
            // 3. Sequence: Roam / Return Home
            
            var attackSequence = new SequenceNode(
                new HasTargetCondition(),
                new SelectorNode(
                    new SequenceNode(
                        new IsTargetInRangeCondition(monster.Monster.AttackRange),
                        new AttackTargetNode()
                    ),
                    new MoveToTargetNode()
                )
            );

            var findTargetSequence = new SequenceNode(
                new InverterNode(new HasTargetCondition()),
                new FindTargetNode()
            );

            // Using dummy nodes for Roaming for now
            var root = new SelectorNode(
                attackSequence,
                findTargetSequence
            );

            Tree = new BehaviorTree(root, blackboard);
        }

        public void Tick()
        {
            Tree.Tick();
        }
    }
}
