using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Map;

namespace NosGm.GameObject.AI.Actions
{
    public class FollowOwnerNode : IBehaviorNode
    {
        public BehaviorStatus Tick(Blackboard blackboard)
        {
            // For pets, "Self" could be Mate or MapMonster
            // Wait, PetAIProfile sets "Self" to Mate
            var mate = blackboard.Get<Mate>("Self");
            if (mate == null || mate.Owner == null || mate.BattleEntity.Hp <= 0)
                return BehaviorStatus.Failure;

            // In NosTale, the client sends pt_ctl packets for pets to follow the owner.
            // So we don't need to do server-side pathfinding for passive following!
            // However, if the server needs to forcefully pull the pet (e.g. teleported),
            // it can just teleport it if it's too far.
            
            if (Map.Map.GetDistance(mate.BattleEntity.GetPos(), mate.Owner.GetPos()) > 20)
            {
                mate.BattleEntity.TeleportTo(mate.Owner.GetPos(), 1);
                return BehaviorStatus.Success;
            }

            // Since client handles smooth walking, we just return Success.
            return BehaviorStatus.Success;
        }
    }
}
