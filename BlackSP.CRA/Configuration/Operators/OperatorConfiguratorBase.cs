using BlackSP.Core.Endpoints;
using BlackSP.CRA.Vertices;
using BlackSP.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration.Operators
{
    public abstract class OperatorConfiguratorBase : IOperatorConfigurator
    {
        /// <summary>
        /// The name of the machine instance where the operator will be executing
        /// </summary>
        public string[] InstanceNames { get; }

        /// <summary>
        /// Equivalent of CRA's 'VertexName'
        /// </summary>
        public string OperatorName { get; }

        /// <summary>
        /// Reference to the Type of the requested BlackSP Operator
        /// </summary>
        public abstract Type OperatorType { get; }

        public abstract Type OperatorConfigurationType { get; }

        public ICollection<string> InputEndpointNames { get; private set; }
        public ICollection<string> OutputEndpointNames { get; private set; }
        public virtual ICollection<Edge> OutgoingEdges { get; private set; }

        public OperatorConfiguratorBase(string[] instanceNames, string operatorName)
        {
            InstanceNames = instanceNames;
            OperatorName = operatorName;
            InputEndpointNames = new List<string>();
            OutputEndpointNames = new List<string>();
            OutgoingEdges = new List<Edge>();
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
    }
}
