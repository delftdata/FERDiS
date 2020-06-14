using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.IoC
{
    [Serializable]

    public class VertexConfiguration : IVertexConfiguration
    {
        /// <summary>
        /// Name of the operator this vertex is part of (only globally unique with 1 shard)
        /// </summary>
        public string OperatorName { get; set; }

        /// <summary>
        /// Name of this vertex (globally unique)
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// The type of the current vertex
        /// </summary>
        public VertexType VertexType { get; set; }

        /// <summary>
        /// Configuration of input endpoints
        /// </summary>
        public ICollection<IEndpointConfiguration> InputEndpoints { get; set; }

        /// <summary>
        /// Configuration of output endpoints.
        /// </summary>
        public ICollection<IEndpointConfiguration> OutputEndpoints { get; set; }
    }
}
