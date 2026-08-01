using NosGm.AI.Core;
using NosGm.GameObject;
using System.Linq;
using NosGm.GameObject.Map;
using NosGm.GameObject.Battle;

namespace NosGm.GameObject.AI.Actions
{
    public class ReturnToSpawnNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var entity = blackboard.Get<MapMonster>("Self");
            if (entity == null) return BehaviorStatus.Failure;

            // Clear aggro completely
            blackboard.Remove("Target");
            entity.Target = null;
            entity.ClearDamageList();

            // Desactivar el objetivo para que no ataque a nadie
            if (entity.IsAlive)
            {
                var spawnPos = new MapCell { X = entity.FirstX, Y = entity.FirstY };
                int dist = Map.Map.GetDistance(entity.GetPos(), spawnPos);

                if (dist <= 2)
                {
                    // Llegó al spawn. 
                    entity.RunToX = 0;
                    entity.RunToY = 0;

                    // Curar 30% de vida (Regeneración de zona base)
                    int healAmount = (int)(entity.BattleEntity.HpMax * 0.30);
                    if (entity.CurrentHp + healAmount > entity.BattleEntity.HpMax)
                    {
                        healAmount = entity.BattleEntity.HpMax - entity.CurrentHp;
                    }

                    if (healAmount > 0)
                    {
                        entity.CurrentHp += healAmount;
                        entity.MapInstance.Broadcast(entity.GenerateRc(healAmount));
                    }

                    return BehaviorStatus.Success;
                }
                
                // Moverse al spawn
                entity.RunToX = entity.FirstX;
                entity.RunToY = entity.FirstY;
                entity.MoveTest();

                return BehaviorStatus.Running;
            }

            return BehaviorStatus.Failure;
        }
    }
}
