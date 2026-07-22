using NosGm.GameObject._Event;
using NosGm.GameObject.Battle;

namespace NosGm.GameObject._BCards.Event
{
    public class BCardEvent : PlayerEvent
    {
        public BattleEntity Target { get; set; }

        public BattleEntity Sender { get; set; }

        public BCard Card { get; set; }
    }
}