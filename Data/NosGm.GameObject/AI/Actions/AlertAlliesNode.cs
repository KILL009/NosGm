using NosGm.AI.Core;
using NosGm.GameObject;

using NosGm.GameObject.Battle;
using System.Linq;

namespace NosGm.GameObject.AI.Actions
{
    public class AlertAlliesNode : IBehaviorNode
    {
        private readonly int _radius;

        public AlertAlliesNode(int radius = 10)
        {
            _radius = radius;
        }

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (entity == null || target == null) return BehaviorStatus.Failure;

            // Only swarm if the target is an enemy
            if (!entity.BattleEntity.CanAttackEntity(target)) return BehaviorStatus.Failure;

            // Find nearby monsters (allies)
            var allies = entity.MapInstance.GetMonsterInRangeList(entity.MapX, entity.MapY, (byte)_radius);

            foreach (var ally in allies)
            {
                if (ally != entity && ally.IsAlive && ally.Target == null)
                {
                    // Only alert monsters of the same faction or neutral that can attack the target
                    if (ally.BattleEntity.CanAttackEntity(target))
                    {
                        // Set their AI Target through Blackboard if they have AI initialized
                        // OR just set ally.Target and they will pick it up on their next Tick
                        ally.Target = target;
                        ally.AiProfile?.Tree?.Blackboard.Set("Target", target);
                    }
                }
            }

            return BehaviorStatus.Success;
        }
    }
}
