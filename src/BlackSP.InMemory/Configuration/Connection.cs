using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.InMemory.Configuration
{
    public class Connection
    {
        public string FromVertexName { get; set; }
        public string FromInstanceName { get; set; }
        public string FromEndpointName { get; set; }
        public int FromShardId { get; set; }
        public int FromShardCount { get; set; }
        public string ToVertexName { get; set; }
        public string ToInstanceName { get; set; }
        public string ToEndpointName { get; set; }
        public int ToShardId { get; set; }
        public int ToShardCount { get; set; }

    }
}
