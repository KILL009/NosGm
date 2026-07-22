using System;

namespace NosGm.GameObject.Extension
{
    /// <summary>
    /// An extension class for the between operation
    /// name pattern IsBetweenXX where X = I -> Inclusive, X = E -> Exclusive
    /// </summary>
    public static class BetweenExtensions
    {
        /// <summary>
        /// Between check <![CDATA[min <= value <= max]]> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">the value to check</param>
        /// <param name="start">Inclusive minimum border</param>
        /// <param name="end">Inclusive maximum border</param>
        /// <returns>return true if the value is between the min & max else false</returns>
        public static bool IsBetween<T>(this T item, T start, T end) where T : IComparable
        {
            return item.CompareTo(start) >= 0 && item.CompareTo(end) <= 0;
        }
    }
}