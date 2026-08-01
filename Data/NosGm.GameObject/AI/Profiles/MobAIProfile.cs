using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;
using System;

namespace NosGm.GameObject.AI.Profiles
{
    public class MobAIProfile : IAIProfile
    {
        public BehaviorTree Tree { get; }

        public MobAIProfile(MapMonster monster)
        {
            if (monster == null)
            {
                throw new ArgumentNullException(nameof(monster));
            }

            if (monster.Monster == null)
            {
                throw new InvalidOperationException(
                    $"Cannot build the AI profile for map monster {monster.MapMonsterId}: NPC/monster definition {monster.MonsterVNum} is missing.");
            }

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
                        new IsTargetInRangeCondition(monster.Monster.BasicRange),
                        new AttackTargetNode(),
                        new global::NosGm.AI.Actions.WaitNode(System.TimeSpan.FromMilliseconds(1500)) // Cooldown de ataque
                    ),
                    new MoveToTargetNode()
                )
            );

            var findTargetSequence = new SequenceNode(
                new InverterNode(new HasTargetCondition()),
                new FindTargetNode()
            );

            var root = new SelectorNode(
                attackSequence,
                findTargetSequence,
                new RoamNode()
            );

            Tree = new BehaviorTree(root, blackboard);
        }

        public void Tick()
        {
            Tree.Tick();
        }
    }
}