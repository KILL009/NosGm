using ChickenAPI.Events;

namespace Frostvein.GameObject._Event
{
    public class PlayerEvent : IEventNotification
    {
        public EventEntity Sender { get; set; }
    }
}