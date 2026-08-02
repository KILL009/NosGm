using NosGm.DAL.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NosGm.DAL.EF.Cache
{
    public class MemoryCacheService<TKey, TValue> : ICacheService<TKey, TValue>
    {
        private volatile ConcurrentDictionary<TKey, CacheItem> _cache = new ConcurrentDictionary<TKey, CacheItem>();
        private readonly Timer _cleanupTimer;
        private readonly Func<TValue, TValue> _cloneFunc;

        private long _cacheHits;
        private long _cacheMisses;
        private long _expiredItems;
        private long _removedItems;
        private long _evictionRuns;
        private long _bulkCacheHits;
        private long _bulkCacheMisses;
        private long _entriesReturned;
        private long _fullReloads;
        private long _invalidations;

        public MemoryCacheService(Func<TValue, TValue> cloneFunc = null)
        {
            _cloneFunc = cloneFunc;
            // Run cleanup every 10 minutes
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        public CacheStatisticsSnapshot GetStatistics()
        {
            long hits = Interlocked.Read(ref _cacheHits);
            long misses = Interlocked.Read(ref _cacheMisses);
            long total = hits + misses;

            return new CacheStatisticsSnapshot
            {
                CacheHits = hits,
                CacheMisses = misses,
                StoredItems = _cache.Count,
                ExpiredItems = Interlocked.Read(ref _expiredItems),
                RemovedItems = Interlocked.Read(ref _removedItems),
                HitRate = total > 0 ? (double)hits / total : 0,
                EvictionRuns = Interlocked.Read(ref _evictionRuns),
                BulkCacheHits = Interlocked.Read(ref _bulkCacheHits),
                BulkCacheMisses = Interlocked.Read(ref _bulkCacheMisses),
                EntriesReturned = Interlocked.Read(ref _entriesReturned),
                FullReloads = Interlocked.Read(ref _fullReloads),
                Invalidations = Interlocked.Read(ref _invalidations)
            };
        }

        public void Clear()
        {
            var count = _cache.Count;
            _cache.Clear();
            Interlocked.Add(ref _removedItems, count);
            Interlocked.Increment(ref _invalidations);
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        public TValue Get(TKey key)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }
            return default;
        }

        public IEnumerable<TValue> GetAll()
        {
            var results = new List<TValue>();
            foreach (var kvp in _cache)
            {
                if (!kvp.Value.IsExpired)
                {
                    kvp.Value.Renew();
                    results.Add(_cloneFunc != null ? _cloneFunc(kvp.Value.Value) : kvp.Value.Value);
                }
                else
                {
                    if (_cache.TryRemove(kvp.Key, out _))
                    {
                        Interlocked.Increment(ref _expiredItems);
                    }
                }
            }
            Interlocked.Increment(ref _bulkCacheHits);
            Interlocked.Add(ref _entriesReturned, results.Count);
            return results;
        }

        public void Remove(TKey key)
        {
            if (_cache.TryRemove(key, out _))
            {
                Interlocked.Increment(ref _removedItems);
            }
        }

        public void Set(TKey key, TValue value)
        {
            _cache[key] = new CacheItem(_cloneFunc != null ? _cloneFunc(value) : value, null);
        }

        public void Set(TKey key, TValue value, TimeSpan expirationTime)
        {
            _cache[key] = new CacheItem(_cloneFunc != null ? _cloneFunc(value) : value, expirationTime);
        }

        public void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            var newCache = new ConcurrentDictionary<TKey, CacheItem>();
            foreach (var item in items)
            {
                newCache[item.Key] = new CacheItem(_cloneFunc != null ? _cloneFunc(item.Value) : item.Value, null);
            }
            
            var oldCount = _cache.Count;
            _cache = newCache;
            
            Interlocked.Add(ref _removedItems, oldCount);
            Interlocked.Increment(ref _fullReloads);
            Interlocked.Increment(ref _bulkCacheMisses);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    item.Renew();
                    Interlocked.Increment(ref _cacheHits);
                    value = _cloneFunc != null ? _cloneFunc(item.Value) : item.Value;
                    return true;
                }
                
                if (_cache.TryRemove(key, out _))
                {
                    Interlocked.Increment(ref _expiredItems);
                }
            }
            
            Interlocked.Increment(ref _cacheMisses);
            value = default;
            return false;
        }

        private void CleanupExpiredItems(object state)
        {
            Interlocked.Increment(ref _evictionRuns);
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    if (_cache.TryRemove(kvp.Key, out _))
                    {
                        Interlocked.Increment(ref _expiredItems);
                    }
                }
            }
        }

        private class CacheItem
        {
            public TValue Value { get; }
            public TimeSpan? SlidingExpiration { get; }
            public DateTime? AbsoluteExpiration { get; private set; }

            public bool IsExpired => AbsoluteExpiration.HasValue && DateTime.UtcNow >= AbsoluteExpiration.Value;

            public CacheItem(TValue value, TimeSpan? slidingExpiration)
            {
                Value = value;
                SlidingExpiration = slidingExpiration;
                if (slidingExpiration.HasValue)
                {
                    AbsoluteExpiration = DateTime.UtcNow.Add(slidingExpiration.Value);
                }
            }

            public void Renew()
            {
                if (SlidingExpiration.HasValue)
                {
                    AbsoluteExpiration = DateTime.UtcNow.Add(SlidingExpiration.Value);
                }
            }
        }
    }
}
