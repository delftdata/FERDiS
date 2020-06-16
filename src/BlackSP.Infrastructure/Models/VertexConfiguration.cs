using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]

    public class VertexConfiguration : IVertexConfiguration
    {
        /// <summary>
        /// Name of the operator this vertex is part of (only globally unique with 1 shard)
        /// </summary>
        public string VertexName { get; set; }

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
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<IEndpointConfiguration> InputEndpoints { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Configuration of output endpoints.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<IEndpointConfiguration> OutputEndpoints { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
