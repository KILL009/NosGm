using Frostvein.GameObject;
using System.Threading.Tasks;
using static Frostvein.Domain.BCardType;

namespace Game.Configuration.BCards
{
    public interface IBCardHandler
    {
        CardType ActionType { get; }

        void Execute(BCardEvent packet);
    }
}