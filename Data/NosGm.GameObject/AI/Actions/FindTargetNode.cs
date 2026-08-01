using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using System;
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

            // Non-hostile mobs (passive animals, town NPCs, etc.) NEVER seek targets
            // on their own — they only fight back when hit (handled by aggro system).
            if (!entity.IsHostile) return BehaviorStatus.Failure;

            // Cap the detection radius to 3 cells max (matches retail NosTale feel).
            // If the DB value is 0 (undefined) use 3 as a safe default.
            byte noticeRange = (byte)Math.Min(entity.Monster.NoticeRange == 0 ? 3 : entity.Monster.NoticeRange, 3);

            // Get closest alive, visible player within notice range
            var target = entity.MapInstance.GetCharactersInRange(entity.MapX, entity.MapY, noticeRange)
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
