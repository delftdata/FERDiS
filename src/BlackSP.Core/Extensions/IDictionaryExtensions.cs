using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Extensions
{
    public static class IDictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, T> collectionMap, string key)
        {
            _ = collectionMap ?? throw new ArgumentNullException(nameof(collectionMap));
            _ = key ?? throw new ArgumentNullException(nameof(key));
            if (collectionMap.TryGetValue(key, out T result))
            {
                return result;
            }
            throw new ArgumentOutOfRangeException(nameof(key), $"No value found for key: {key}.");

        }
    }
}
