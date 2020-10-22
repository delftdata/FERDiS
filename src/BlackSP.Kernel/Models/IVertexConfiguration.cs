using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public enum VertexType
    {
        Source,
        Operator,
        Coordinator
    }

    public interface IVertexConfiguration
    {
        /// <summary>
        /// Name of the vertex this instance is part of. (is globally unique iff 1 shard)
        /// </summary>
        string VertexName { get; }

        /// <summary>
        /// Instancenames this vertex is running on (globally unique)
        /// </summary>
        IEnumerable<string> InstanceNames { get; }

        /// <summary>
        /// Instance name that is currently hosting this vertex
        /// </summary>
        string InstanceName { get; }

        /// <summary>
        /// The shard of the current running instance
        /// </summary>
        int ShardId { get; }

        /// <summary>
        /// The type of the current vertex
        /// </summary>
        VertexType VertexType { get; }

        /// <summary>
        /// Configuration of input endpoints
        /// </summary>
        ICollection<IEndpointConfiguration> InputEndpoints { get; }

        /// <summary>
        /// Configuration of output endpoints.
        /// </summary>
        ICollection<IEndpointConfiguration> OutputEndpoints { get; }

        /// <summary>
        /// Sets the shard ID this vertex is taking on. Affects the InstanceName property.
        /// </summary>
        /// <param name="shardId"></param>
        void SetCurrentShardId(int shardId);
    }

    
}
