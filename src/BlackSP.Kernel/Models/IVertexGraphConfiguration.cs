using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IVertexGraphConfiguration
    {
        /// <summary>
        /// Contains instance names of every instance in the vertex graph
        /// </summary>
        IEnumerable<string> InstanceNames { get; set; }

        /// <summary>
        /// Contains tuples {from, to} where each string represents an instanceName
        /// </summary>
        IEnumerable<Tuple<string, string>> InstanceConnections { get; set; }
    }
}
