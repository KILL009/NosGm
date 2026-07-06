using System.Collections.Concurrent;

namespace Frostvein.Core
{
    public static class ConcurrentQueueExtensions
    {
        #region Methods

        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.Count > 0)
                queue.TryDequeue(out var item);
        }

        #endregion
    }
}