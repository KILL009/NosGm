using global::NosGm.AI.Core;
using global::NosGm.AI.Composites;
using global::NosGm.AI.Decorators;
using NosGm.GameObject;
using NosGm.GameObject.AI.Actions;
using NosGm.GameObject.AI.Conditions;

namespace NosGm.GameObject.AI.Profiles
{
    public class BossAIProfile
    {
        public BehaviorTree Tree { get; }

        public BossAIProfile(MapMonster boss)
        {
            var blackboard = new Blackboard();
            blackboard.Set("Self", boss);

            // Bosses usually have phase transitions or multiple skills
            // To be implemented: Sequence of advanced boss actions
            Tree = new BehaviorTree(new global::NosGm.AI.Decorators.InverterNode(null), blackboard); // Placeholder
        }

        public void Tick() => Tree.Tick();
    }
}
