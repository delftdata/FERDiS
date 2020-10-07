using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            _ = item ?? throw new ArgumentNullException(nameof(item));
            yield return item;
        }

        /// <summary>
        /// Coalesces over a potentially null ienumerable and returns an empty enumerable if it was null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="potentialNull"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrEmpty<T> (this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }
    }
}
