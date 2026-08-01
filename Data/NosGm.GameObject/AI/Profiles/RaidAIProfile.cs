using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;

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
            Tree = new BehaviorTree(new global::NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
