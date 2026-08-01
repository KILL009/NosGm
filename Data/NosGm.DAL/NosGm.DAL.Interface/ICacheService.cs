using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface ICacheService<TKey, TValue>
    {
        TValue Get(TKey key);
        void Set(TKey key, TValue value);
        void Set(TKey key, TValue value, TimeSpan expirationTime);
        bool TryGetValue(TKey key, out TValue value);
        void Remove(TKey key);
        void Clear();
        IEnumerable<TValue> GetAll();
        bool ContainsKey(TKey key);
    }
}
