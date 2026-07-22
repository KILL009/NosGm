using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NosGm.Core
{
    public static class ConcurrentBagExtensions
    {
        #region Methods

        public static void Clear<T>(this ConcurrentBag<T> queue)
        {
            while (queue.Count > 0)
                queue.TryTake(out var item);
        }

        public static void RemoveWhere<T>(this ConcurrentBag<T> queue, Func<T, bool> predicate,
            out ConcurrentBag<T> queueReturned)
        {
            queueReturned = new ConcurrentBag<T>(queue.Where(predicate));
        }

        public static ConcurrentBag<T> Replace<T>(this ConcurrentBag<T> queue, Func<T, bool> predicate)
        {
            return new ConcurrentBag<T>(queue.ToList().Where(predicate));
        }

        #endregion
    }
}