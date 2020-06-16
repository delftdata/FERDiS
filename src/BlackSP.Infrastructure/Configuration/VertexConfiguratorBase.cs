using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public abstract class VertexConfiguratorBase : IVertexConfigurator
    {
        /// <summary>
        /// The name of the machine instance where the operator will be executing
        /// </summary>
        public ICollection<string> InstanceNames { get; }

        /// <summary>
        /// Equivalent of CRA's 'VertexName'
        /// </summary>
        public string VertexName { get; }

        public abstract VertexType VertexType { get; }
        public abstract Type ModuleType { get; }

        public virtual ICollection<Edge> OutgoingEdges { get; private set; }
        public virtual ICollection<Edge> IncomingEdges { get; private set; }

        private ICollection<string> InputEndpointNames { get; set; }
        private ICollection<string> OutputEndpointNames { get; set; }


        public VertexConfiguratorBase(string[] instanceNames, string vertexName)
        {
            InstanceNames = instanceNames;
            VertexName = vertexName;
            OutgoingEdges = new List<Edge>();
            IncomingEdges = new List<Edge>();

            InputEndpointNames = new List<string>();
            OutputEndpointNames = new List<string>();
        }

        public string GetAvailableInputEndpoint()
        {
            string inputEndpointName = $"input{InputEndpointNames.Count}";
            InputEndpointNames.Add(inputEndpointName);
            return inputEndpointName;
        }

        public string GetAvailableOutputEndpoint()
        {
            string outputEndpointName = $"output{OutputEndpointNames.Count}";
            OutputEndpointNames.Add(outputEndpointName);
            return outputEndpointName;
        }

        /// <summary>
        /// Transforms configurator to a set of IVertexConfigurations to be passed to blacksp vertices
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<IVertexConfiguration> ToConfigurations()
        {
            foreach (var instanceName in InstanceNames)
            {
                yield return new VertexConfiguration()
                {
                    InstanceName = instanceName,
                    VertexName = VertexName,
                    VertexType = VertexType,
                    InputEndpoints = IncomingEdges.Select(Edge.AsEndpointConfiguration).ToList(),
                    OutputEndpoints = OutgoingEdges.Select(Edge.AsEndpointConfiguration).ToList(),
                };
            }
        }
    }
}
