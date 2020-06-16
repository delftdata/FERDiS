using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    public class EndpointConfiguration : IEndpointConfiguration
    {
        public string LocalEndpointName { get; set; }

        public string RemoteVertexName { get; set; }

        public string RemoteEndpointName { get; set; }

        public int RemoteShardCount { get; set; }

        public bool IsControl { get; set; }

        public string GetConnectionKey(int shardId)
        {
            if(shardId < RemoteShardCount && shardId > -1)
            {
                return $"{RemoteVertexName}{RemoteEndpointName}{shardId}";
            }
            throw new ArgumentException($"invalid value: {shardId}", nameof(shardId));
        }
    }
}
