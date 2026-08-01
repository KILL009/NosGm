using NosGm.AI.Core;
using NosGm.GameObject;

namespace NosGm.GameObject.AI.Profiles
{
    public class RaidAIProfile
    {
        public BehaviorTree Tree { get; }

        public RaidAIProfile(MapMonster raidBoss)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", raidBoss);

            // Raid AI: Button mechanics, spawns, invulnerability phases
            Tree = new BehaviorTree(new NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
