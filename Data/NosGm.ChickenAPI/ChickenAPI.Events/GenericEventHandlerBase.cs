using System;
using System.Threading;

namespace ChickenAPI.Events
{
    public abstract class GenericEventHandlerBase<TNotification> : IEventHandler
        where TNotification : IEventNotification
    {
        public Type Type => typeof(TNotification);

        public void Handle(IEventNotification notification, CancellationToken cancellation)
        {
            if (notification is TNotification typedNotification)
            {
                Handle(typedNotification, cancellation);
            }
        }

        protected abstract void Handle(TNotification e, CancellationToken cancellation);
    }
}