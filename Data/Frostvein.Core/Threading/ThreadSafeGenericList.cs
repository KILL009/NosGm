using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Frostvein.Core
{
    public class ThreadSafeGenericList<T> : IDisposable
    {
        #region Instantiation

        /// <summary>
        ///     Creates a new ThreadSafeGenericList object.
        /// </summary>
        public ThreadSafeGenericList()
        {
            _items = new List<T>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="List{T}" />.
        /// </summary>
        public int Count
        {
            get
            {
                if (!_disposed)
                {
                    _lock.EnterReadLock();
                    try
                    {
                        return _items.Count;
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }

                return 0;
            }
        }

        #endregion

        #region Members

        /// <summary>
        ///     private collection to store _items.
        /// </summary>
        private readonly List<T> _items;

        /// <summary>
        ///     Used to synchronize access to _items list.
        /// </summary>
        private readonly ReaderWriterLockSlim _lock;

        private bool _disposed;

        #endregion

        #region Methods

        /// <summary>
        ///     Adds an object to the end of the <see cref="List{T}" />.
        /// </summary>
        /// <param name="value"></param>
        public void Add(T value)
        {
            if (!_disposed)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Add(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        

        /// <summary>
        ///     Adds the elements of the specified collection to the end of the <see cref="List{T}" />.
        /// </summary>
        /// <param name="value"></param>
        public void AddRange(List<T> value)
        {
            if (!_disposed)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.AddRange(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Determines whether all elements of a sequence satisfy a condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="bool" /> True; if elements satisfy the condition</returns>
        public bool All(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.All(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return false;
        }

        /// <summary>
        ///     Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>
        ///     <see cref="bool" />
        /// </returns>
        public bool Any(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Any(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return false;
        }

        /// <summary>
        ///     Removes all elements from the <see cref="List{T}" />.
        /// </summary>
        public void Clear()
        {
            if (!_disposed)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Copies the entire <see cref="List{T}" /> to a compatible one-dimensional array, starting
        ///     at the beginning of the target array.
        /// </summary>
        /// <param name="grpmembers"></param>
        public void CopyTo(T[] grpmembers)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    _items.CopyTo(grpmembers);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="int" /> number of found elements</returns>
        public int CountLinq(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Count(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Disposes the current object.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///     Returns the element at given index
        /// </summary>
        /// <param name="v"></param>
        /// <returns><see cref="T" /> object</returns>
        public T ElementAt(int v)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items[v];
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Searches for an element that matches the conditions defined by the specified predicate,
        ///     and returns the first occurrence within the entire <see cref="List{T}" />.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="T" /> object</returns>
        public T Find(Predicate<T> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Find(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Searches for an element that matches the conditions defined by the specified predicate,
        ///     and returns the first occurrence within the entire <see cref="List{T}" />.
        /// </summary>
        /// <returns><see cref="T" /> object</returns>
        public T FirstOrDefault()
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.FirstOrDefault();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Performs the specified action on each element of the <see cref="List{T}" />.
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(Action<T> action)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    _items.ForEach(action);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     returns a list of all objects in current thread safe generic list
        /// </summary>
        /// <returns>
        ///     <see cref="List{T}" />
        /// </returns>
        public List<T> GetAllItems()
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return new List<T>(_items);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return new List<T>();
        }

        /// <summary>
        ///     Returns the last element of a sequence that satisfies a condition or a default value if
        ///     no such element is found.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="T" /> object</returns>
        public T LastOrDefault(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.LastOrDefault(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Removes the first occurrence of a specific object from the <see cref="List{T}" />.
        /// </summary>
        /// <param name="match"></param>
        public void Remove(T match)
        {
            if (!_disposed)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.Remove(match);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match"></param>
        public void RemoveAll(Predicate<T> match)
        {
            if (!_disposed)
            {
                _lock.EnterWriteLock();
                try
                {
                    _items.RemoveAll(match);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Returns the only element of a sequence that satisfies a specified condition, and throws
        ///     an exception if more than one such element exists.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="T" /> object</returns>
        public T Single(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Single(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Returns the only element of a sequence that satisfies a specified condition or a default
        ///     value if no such element exists; this method throws an exception if more than one element
        ///     satisfies the condition.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="T" /> object</returns>
        /// <exception cref="InvalidOperationException" />
        public T SingleOrDefault(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.SingleOrDefault(predicate);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return default;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns><see cref="int" /> number of found elements</returns>
        public int Sum(Func<T, int> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns>int? number of found elements</returns>
        public int? Sum(Func<T, int?> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns><see cref="long" /> number of found elements</returns>
        public long Sum(Func<T, long> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns>long? number of found elements</returns>
        public long? Sum(Func<T, long?> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns><see cref="double" /> number of found elements</returns>
        public double Sum(Func<T, double> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        /// <summary>
        ///     Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns>double? number of found elements</returns>
        public double? Sum(Func<T, double?> selector)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Sum(selector);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return 0;
        }

        public List<T> ToList()
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();

                try
                {
                    return _items.ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return new List<T>();
        }

        /// <summary>
        ///     Filters a sequence of values based on a predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>
        ///     <see cref="List{T}" />
        /// </returns>
        public List<T> Where(Func<T, bool> predicate)
        {
            if (!_disposed)
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Where(predicate).ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return new List<T>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
                _lock.Dispose();
            }
        }

        #endregion
    }
}