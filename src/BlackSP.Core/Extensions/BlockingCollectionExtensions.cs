using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Extensions
{
    public static class BlockingCollectionExtensions
    {
        public static T Get<T>(this IDictionary<string, T> collectionMap, string endpointKey)
        {
            _ = collectionMap ?? throw new ArgumentNullException(nameof(collectionMap));
            _ = endpointKey ?? throw new ArgumentNullException(nameof(endpointKey));

            if (collectionMap.TryGetValue(endpointKey, out T result))
            {
                return result;
            }
            throw new ArgumentOutOfRangeException(nameof(endpointKey), $"No control or data shard-queue found for endpoint: {endpointKey}");

        }
    }
}
