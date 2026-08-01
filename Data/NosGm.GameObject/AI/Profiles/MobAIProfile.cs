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
            
            // Ajustar el rango de ataque: si BasicRange es 0 usar 1.
            // Los rangos a distancia (BasicRange > 0) se respetan tal cual de la base de datos.
            // Esto garantiza que los mobs cuerpo a cuerpo puedan atacar desde celdas adyacentes
            // sin necesitar estar en la misma celda exacta que el jugador.
            int attackRange = monster.Monster.BasicRange <= 0 ? 1 : monster.Monster.BasicRange;

            var attackSequence = new SequenceNode(
                new HasTargetCondition(),
                new SelectorNode(
                    new SequenceNode(
                        new IsTargetInRangeCondition(attackRange),
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