using NosGm.GameObject.Battle;
using System.Threading.Tasks;

namespace NosGm.GameObject._BCards
{
    public interface IBCardEffectHandlerContainer
    {
        Task RegisterAsync(IBCardEffectAsyncHandler handler);

        Task UnregisterAsync(IBCardEffectAsyncHandler handler);

        void Execute(BattleEntity target, BattleEntity sender, BCard bcard);
    }
}