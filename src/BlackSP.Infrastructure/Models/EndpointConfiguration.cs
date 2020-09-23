using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Infrastructure.Models
{

    [Serializable]
    public class EndpointConfiguration : IEndpointConfiguration
    {
        public string LocalEndpointName { get; set; }

        public string RemoteVertexName { get; set; }

        public string RemoteEndpointName { get; set; }

        public bool IsControl { get; set; }

        public IEnumerable<string> RemoteInstanceNames { get; set; }

        public string GetConnectionKey(int shardId)
        {
            //TODO: consider using remote instanceName as key?
            if(shardId < RemoteInstanceNames.Count() && shardId > -1)
            {
                return $"{RemoteInstanceNames.ElementAt(shardId)}{RemoteVertexName}{RemoteEndpointName}{shardId}";
            }
            throw new ArgumentException($"invalid value: {shardId}", nameof(shardId));
        }
    }
}
