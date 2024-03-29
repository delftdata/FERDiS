﻿using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    public class TestEndpointConfiguration : IEndpointConfiguration
    {
        public string LocalEndpointName { get; set; }

        public string RemoteVertexName { get; set; }

        public string RemoteEndpointName { get; set; }

        public bool IsControl { get; set; }

        public IEnumerable<string> RemoteInstanceNames { get; set; }

        public bool IsPipeline { get; set; }

        public bool IsBackchannel { get; set; }

        public string GetConnectionKey(int shardId)
        {
            if (shardId < RemoteInstanceNames.Count() && shardId > -1)
            {
                return $"{RemoteInstanceNames.ElementAt(shardId)}{RemoteVertexName}{RemoteEndpointName}{shardId}";
            }
            throw new ArgumentException($"invalid value: {shardId}", nameof(shardId));
        }

        public string GetRemoteInstanceName(int shardId)
        {
            return RemoteInstanceNames.ElementAt(shardId);
        }
    }
}
