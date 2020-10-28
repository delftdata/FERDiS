using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Models;
using BlackSP.Simulator.Configuration;
using System;
using System.Collections.Generic;

namespace BlackSP.Simulator.Extensions
{
    public static class EdgeBuilderExtensions
    {
        /// <summary>
        /// Transforms an edge between endpoints to a set of connections from shard to shard.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static IEnumerable<Connection> ToConnections(this IEdgeBuilder edge)
        {
            _ = edge ?? throw new ArgumentNullException(nameof(edge));
            int fromShardId = 0;
            foreach(var fromInstanceName in edge.FromVertex.InstanceNames)
            {
                int toShardId = 0;
                foreach(var toInstanceName in edge.ToVertex.InstanceNames)
                {
                    //TODO: consider if necessary, in CRA we just ignore superfluous connections
                    //if(edge.IsShuffle() || (edge.IsPipeline() && toShardId == fromShardId))
                    //{
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
                    //}
                    toShardId++;
                }
                fromShardId++;
            }
        }
    }
}
