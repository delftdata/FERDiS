using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
