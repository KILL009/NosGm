using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Exception = System.Exception;

namespace NosGm.Core.Threading
{
    public class ThreadSafeLockedDictionary<T, TU>
    {
        private readonly Dictionary<T, TU> _internalDictionary;

        public ThreadSafeLockedDictionary()
        {
            _internalDictionary = new Dictionary<T, TU>();
        }

        public ThreadSafeLockedDictionary(IDictionary<T, TU> other)
        {
            _internalDictionary = other.ToDictionary(x => x.Key, v => v.Value);
        }

        public int Count
        {
            get
            {
                lock (_internalDictionary)
                {
                    return _internalDictionary.Count;
                }
            }
        }

        public bool TryAdd(T key, TU value)
        {
            try
            {
                lock (_internalDictionary)
                {
                    if (_internalDictionary.ContainsKey(key))
                    {
                        return false;
                    }

                    _internalDictionary.Add(key, value);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool TryRemove(T key, out TU value)
        {
            try
            {
                lock (_internalDictionary)
                {
                    if (!_internalDictionary.ContainsKey(key))
                    {
                        value = default;
                        return false;
                    }

                    value = _internalDictionary[key];
                    _internalDictionary.Remove(key);
                    return true;
                }
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public bool ContainsKey(T key)
        {
            lock (_internalDictionary)
            {
                return _internalDictionary.ContainsKey(key);
            }
        }

        public TU this[T key]
        {
            get
            {
                lock (_internalDictionary)
                {
                    return _internalDictionary[key];
                }
            }
            set
            {
                lock (_internalDictionary)
                {
                    _internalDictionary[key] = value;
                }
            }
        }

        public ICollection<TU> Values
        {
            get
            {
                lock (_internalDictionary)
                {
                    return _internalDictionary.Values;
                }
            }
        }

        public ICollection<T> Keys
        {
            get
            {
                lock (_internalDictionary)
                {
                    return _internalDictionary.Keys;
                }
            }
        }
    }
}
