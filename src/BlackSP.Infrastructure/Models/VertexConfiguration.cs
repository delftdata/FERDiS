using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class VertexConfiguration : IVertexConfiguration
    {
        /// <inheritdoc/>
        public string VertexName { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> InstanceNames { get; set; }

        /// <inheritdoc/>
        public string InstanceName => _currentInstanceName ?? string.Empty;
        private string _currentInstanceName;

        public int ShardId { get; set; }


        /// <inheritdoc/>
        public VertexType VertexType { get; set; }

        /// <inheritdoc/>
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<IEndpointConfiguration> InputEndpoints { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <inheritdoc/>
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<IEndpointConfiguration> OutputEndpoints { get; set; }

#pragma warning restore CA2227 // Collection properties should be read only

        public void SetCurrentShardId(int shardId)
        {
            ShardId = shardId;
            _currentInstanceName = InstanceNames.ElementAt(shardId);
        }
    }
}
