using NosGm.AI.Core;

namespace NosGm.GameObject.AI.Profiles
{
    public interface IAIProfile
    {
        BehaviorTree Tree { get; }
        void Tick();
    }
}
