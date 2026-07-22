using ChickenAPI.Events;

namespace NosGm.GameObject._Event
{
    public class PlayerEvent : IEventNotification
    {
        public EventEntity Sender { get; set; }
    }
}