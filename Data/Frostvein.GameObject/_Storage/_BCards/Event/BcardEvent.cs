using Frostvein.GameObject._Event;
using Frostvein.GameObject.Battle;

namespace Frostvein.GameObject._BCards.Event
{
    public class BCardEvent : PlayerEvent
    {
        public BattleEntity Target { get; set; }

        public BattleEntity Sender { get; set; }

        public BCard Card { get; set; }
    }
}