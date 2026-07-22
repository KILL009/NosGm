using NosGm.Domain;
using NosGm.GameObject.Battle;
using System.Threading.Tasks;

namespace NosGm.GameObject._BCards
{
    public interface IBCardEffectAsyncHandler
    {
        BCardType.CardType HandledType { get; }

        Task ExecuteAsync(BattleEntity target, BattleEntity sender, BCard bcard, short x, short y, short partnerBuffLevel);
    }
}