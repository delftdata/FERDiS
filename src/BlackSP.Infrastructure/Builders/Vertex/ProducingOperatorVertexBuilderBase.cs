
using BlackSP.Infrastructure.Models;
using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public abstract class ProducingOperatorVertexBuilderBase<T> : VertexBuilderBase, IProducingOperatorVertexBuilder<T>
    {

        public ProducingOperatorVertexBuilderBase(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }

        public void Append(IConsumingOperatorVertexBuilder<T> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }

        public void Append<T2>(IConsumingOperatorVertexBuilder<T, T2> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }

        public void Append<T2>(IConsumingOperatorVertexBuilder<T2, T> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }
    }
}
