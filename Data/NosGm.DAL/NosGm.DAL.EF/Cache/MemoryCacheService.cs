using NosGm.DAL.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosGm.DAL.EF.Cache
{
    public class MemoryCacheService<TKey, TValue> : ICacheService<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, CacheItem> _cache = new ConcurrentDictionary<TKey, CacheItem>();

        public void Clear()
        {
            _cache.Clear();
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
                    results.Add(kvp.Value.Value);
                }
                else
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
            return results;
        }

        public void Remove(TKey key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Set(TKey key, TValue value)
        {
            _cache[key] = new CacheItem(value, null);
        }

        public void Set(TKey key, TValue value, TimeSpan expirationTime)
        {
            _cache[key] = new CacheItem(value, DateTime.UtcNow.Add(expirationTime));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    value = item.Value;
                    return true;
                }
                _cache.TryRemove(key, out _);
            }
            
            value = default;
            return false;
        }

        private class CacheItem
        {
            public TValue Value { get; }
            public DateTime? Expiration { get; }

            public bool IsExpired => Expiration.HasValue && DateTime.UtcNow >= Expiration.Value;

            public CacheItem(TValue value, DateTime? expiration)
            {
                Value = value;
                Expiration = expiration;
            }
        }
    }
}
