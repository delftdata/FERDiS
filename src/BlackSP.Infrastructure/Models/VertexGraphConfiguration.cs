using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class VertexGraphConfiguration : IVertexGraphConfiguration
    {
        public IEnumerable<string> InstanceNames { get; set; }
        public IEnumerable<Tuple<string, string>> InstanceConnections { get; set; }

        public VertexGraphConfiguration(IEnumerable<string> instanceNames, IEnumerable<Tuple<string, string>> instanceConnections)
        {
            InstanceNames = instanceNames;
            InstanceConnections = instanceConnections;
        }
    }
}
