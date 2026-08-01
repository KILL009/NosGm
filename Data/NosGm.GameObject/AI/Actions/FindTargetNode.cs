using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using System.Linq;

namespace NosGm.GameObject.AI.Actions
{
    public class FindTargetNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            if (entity == null) return BehaviorStatus.Failure;

            if (entity.MapInstance == null) return BehaviorStatus.Failure;

            // Simplified Target Finding (Gets closest player in notice range)
            var target = entity.MapInstance.GetCharactersInRange(entity.MapX, entity.MapY, (byte)entity.Monster.NoticeRange)
                .Where(c => c.Hp > 0 && !c.Invisible && !c.InvisibleGm)
                .FirstOrDefault();

            if (target != null)
            {
                blackboard.Set<BattleEntity>("Target", target.BattleEntity);
                return BehaviorStatus.Success;
            }

            return BehaviorStatus.Failure;
        }
    }
}
