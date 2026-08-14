using System.Threading;
using System.Threading.Tasks;

namespace ChickenAPI.Events
{
    /// <summary>
    ///     Defines a handler for any type of notification.
    /// </summary>
    public interface IEventHandler
    {
        void Handle(IEventNotification notification, CancellationToken cancellation);
    }

    /// <summary>
    ///     Optional asynchronous event-handler contract. The event pipeline awaits
    ///     implementations of this interface so callers can safely sequence teardown
    ///     after persistence without relying on async-void handlers.
    /// </summary>
    public interface IAsyncEventHandler : IEventHandler
    {
        Task HandleAsync(IEventNotification notification, CancellationToken cancellation);
    }
}
