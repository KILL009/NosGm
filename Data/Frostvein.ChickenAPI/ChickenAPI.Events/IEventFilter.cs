using System.Threading;
using System.Threading.Tasks;

namespace ChickenAPI.Events
{
    public interface IEventFilter
    {
        /// <summary>
        ///     Handles the preprocessor
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Handle(IEventNotification notification, CancellationToken token);
    }
}