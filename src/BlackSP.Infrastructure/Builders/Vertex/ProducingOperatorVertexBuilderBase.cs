
using BlackSP.Infrastructure.Builders.Edge;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Configuration;

using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public abstract class ProducingOperatorVertexBuilderBase<T> : VertexBuilderBase, IProducingOperatorVertexBuilder<T>
    {

        public ProducingOperatorVertexBuilderBase(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }

        public IEdgeBuilder Append(IConsumingOperatorVertexBuilder<T> otherVertex)
        {
            return AppendGeneric(otherVertex);
        }

        public IEdgeBuilder Append<T2>(IConsumingOperatorVertexBuilder<T, T2> otherVertex)
        {
            return AppendGeneric(otherVertex);
        }

        public IEdgeBuilder Append<T2>(IConsumingOperatorVertexBuilder<T2, T> otherVertex)
        {
            return AppendGeneric(otherVertex);
        }

        private IEdgeBuilder AppendGeneric(IVertexBuilder otherVertex)
        {
            _ = otherVertex ?? throw new ArgumentNullException(nameof(otherVertex));
            var edge = new EdgeBuilder(this, GetAvailableOutputEndpoint(), otherVertex, otherVertex.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge.AsShuffle()); //note: default behavior is shuffle connection
            otherVertex.IncomingEdges.Add(edge);
            return edge;
        }
    }
}
