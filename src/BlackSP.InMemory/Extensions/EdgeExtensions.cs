using BlackSP.Infrastructure.Configuration;
using BlackSP.InMemory.Configuration;
using BlackSP.InMemory.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.InMemory.Extensions
{
    public static class EdgeExtensions
    {
        /// <summary>
        /// Transforms an edge between endpoints to a set of connections from shard to shard.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static IEnumerable<Connection> ToConnections(this Edge edge)
        {
            int fromShardId = 0;
            foreach(var fromInstanceName in edge.FromOperator.InstanceNames)
            {
                int toShardId = 0;
                foreach(var toInstanceName in edge.ToOperator.InstanceNames)
                {
                    yield return new Connection
                    {
                        FromEndpointName = edge.FromEndpoint,
                        FromOperatorName = edge.FromOperator.OperatorName,
                        FromInstanceName = fromInstanceName,
                        FromShardId = fromShardId,
                        FromShardCount = edge.FromOperator.InstanceNames.Length,

                        ToEndpointName = edge.ToEndpoint,
                        ToOperatorName = edge.ToOperator.OperatorName,
                        ToInstanceName = toInstanceName,
                        ToShardId = toShardId,
                        ToShardCount = edge.ToOperator.InstanceNames.Length
                    };
                    toShardId++;
                }
                fromShardId++;
            }
        }
    }
}
