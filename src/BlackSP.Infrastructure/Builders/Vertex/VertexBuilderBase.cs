using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public abstract class VertexBuilderBase : IVertexBuilder
    {
        /// <summary>
        /// The name of the machine instance where the operator will be executing
        /// </summary>
        public ICollection<string> InstanceNames { get; }

        /// <summary>
        /// Name of the vertex
        /// </summary>
        public string VertexName { get; }

        public abstract VertexType VertexType { get; }
        public abstract Type ModuleType { get; }

        public virtual ICollection<Edge> OutgoingEdges { get; private set; }
        public virtual ICollection<Edge> IncomingEdges { get; private set; }

        private ICollection<string> InputEndpointNames { get; set; }
        private ICollection<string> OutputEndpointNames { get; set; }


        public VertexBuilderBase(string[] instanceNames, string vertexName)
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
        public virtual IVertexConfiguration GetVertexConfiguration()
        {
            return new VertexConfiguration()
            {
                InstanceNames = InstanceNames.ToArray(),
                VertexName = VertexName,
                VertexType = VertexType,
                InputEndpoints = IncomingEdges.Select(e => AsEndpointConfiguration(e, true)).ToList(),
                OutputEndpoints = OutgoingEdges.Select(e => AsEndpointConfiguration(e, false)).ToList(),
            };
        }

        private static IEndpointConfiguration AsEndpointConfiguration(Edge edge, bool asInput)
        {
            _ = edge ?? throw new ArgumentNullException(nameof(edge));
            bool fromCoordinator = edge.FromVertex.VertexType == VertexType.Coordinator;
            bool toCoordinator = edge.ToVertex.VertexType == VertexType.Coordinator;

            return new EndpointConfiguration()
            {
                IsControl = fromCoordinator || toCoordinator,
                LocalEndpointName = asInput ? edge.ToEndpoint : edge.FromEndpoint,
                RemoteVertexName = asInput ? edge.FromVertex.VertexName : edge.ToVertex.VertexName,
                RemoteEndpointName = asInput ? edge.FromEndpoint : edge.ToEndpoint,
                RemoteInstanceNames = asInput ? edge.FromVertex.InstanceNames : edge.ToVertex.InstanceNames
            };
        }
    }
}
