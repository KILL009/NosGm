using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public class CacheStatisticsSnapshot
    {
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public long StoredItems { get; set; }
        public long ExpiredItems { get; set; }
        public long RemovedItems { get; set; }
        public double HitRate { get; set; }
        public long EvictionRuns { get; set; }
        public long BulkCacheHits { get; set; }
        public long BulkCacheMisses { get; set; }
        public long EntriesReturned { get; set; }
        public long FullReloads { get; set; }
        public long Invalidations { get; set; }
    }

    public interface ICacheService<TKey, TValue>
    {
        TValue Get(TKey key);
        void Set(TKey key, TValue value);
        void Set(TKey key, TValue value, TimeSpan expirationTime);
        bool TryGetValue(TKey key, out TValue value);
        void Remove(TKey key);
        void Clear();
        void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> items);
        IEnumerable<TValue> GetAll();
        bool ContainsKey(TKey key);

        CacheStatisticsSnapshot GetStatistics();
    }
}
