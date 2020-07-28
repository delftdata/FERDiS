using BlackSP.Infrastructure.Models;
using BlackSP.Simulator.Configuration;
using System.Collections.Generic;

namespace BlackSP.Simulator.Extensions
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
            foreach(var fromInstanceName in edge.FromVertex.InstanceNames)
            {
                int toShardId = 0;
                foreach(var toInstanceName in edge.ToVertex.InstanceNames)
                {
                    yield return new Connection
                    {
                        FromEndpointName = edge.FromEndpoint,
                        FromVertexName = edge.FromVertex.VertexName,
                        FromInstanceName = fromInstanceName,
                        FromShardId = fromShardId,
                        FromShardCount = edge.FromVertex.InstanceNames.Count,

                        ToEndpointName = edge.ToEndpoint,
                        ToVertexName = edge.ToVertex.VertexName,
                        ToInstanceName = toInstanceName,
                        ToShardId = toShardId,
                        ToShardCount = edge.ToVertex.InstanceNames.Count
                    };
                    toShardId++;
                }
                fromShardId++;
            }
        }
    }
}
