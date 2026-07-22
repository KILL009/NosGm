using NosGm.GameObject;
using System.Threading.Tasks;
using static NosGm.Domain.BCardType;

namespace Game.Configuration.BCards
{
    public interface IBCardHandler
    {
        CardType ActionType { get; }

        void Execute(BCardEvent packet);
    }
}