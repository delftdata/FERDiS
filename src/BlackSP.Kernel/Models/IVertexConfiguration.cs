using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IVertexConfiguration
    {
        /// <summary>
        /// Name of the vertex this instance is part of. (is globally unique iff 1 shard)
        /// </summary>
        string VertexName { get; }

        /// <summary>
        /// Name of this vertex (globally unique)
        /// </summary>
        string InstanceName { get; }

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
    }

    public enum VertexType
    {
        Source,
        Operator,
        Coordinator
    }
}
