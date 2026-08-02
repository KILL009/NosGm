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
            if (entity?.MapInstance == null)
            {
                return BehaviorStatus.Failure;
            }

            // Aggro created by combat has priority. This includes mates, so a pet that
            // attacks or receives threat can actually tank instead of every monster
            // snapping back to the owner.
            BattleEntity target = entity.AggroList?
                .Where(candidate => candidate != null &&
                                    candidate.Hp > 0 &&
                                    candidate.MapInstance == entity.MapInstance &&
                                    entity.BattleEntity.CanAttackEntity(candidate))
                .OrderBy(candidate => Map.GetDistance(entity.GetPos(), candidate.GetPos()))
                .FirstOrDefault();

            if (target == null)
            {
                // Passive monsters do not acquire a fresh target, but they can still
                // fight an existing aggro target selected above.
                if (!entity.IsHostile)
                {
                    entity.Target = null;
                    return BehaviorStatus.Failure;
                }

                byte noticeRange = (byte)Math.Min(
                    entity.Monster.NoticeRange == 0 ? 3 : entity.Monster.NoticeRange,
                    3);

                target = entity.MapInstance
                    .GetCharactersInRange(entity.MapX, entity.MapY, noticeRange)
                    .Where(character => character.Hp > 0 &&
                                        !character.Invisible &&
                                        !character.InvisibleGm &&
                                        entity.BattleEntity.CanAttackEntity(character.BattleEntity))
                    .OrderBy(character => Map.GetDistance(entity.GetPos(), character.BattleEntity.GetPos()))
                    .Select(character => character.BattleEntity)
                    .FirstOrDefault();
            }

            if (target == null)
            {
                entity.Target = null;
                return BehaviorStatus.Failure;
            }

            entity.Target = target;
            blackboard.Set("Target", target);
            return BehaviorStatus.Success;
        }
    }
}
