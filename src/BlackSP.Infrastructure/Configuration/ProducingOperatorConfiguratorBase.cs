
using BlackSP.Infrastructure.Models;
using System;

namespace BlackSP.Infrastructure.Configuration
{
    public abstract class ProducingOperatorConfiguratorBase<T> : OperatorConfiguratorBase, IProducingOperatorConfigurator<T>
    {

        public ProducingOperatorConfiguratorBase(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }

        public void Append(IConsumingOperatorConfigurator<T> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }

        public void Append<T2>(IConsumingOperatorConfigurator<T, T2> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }

        public void Append<T2>(IConsumingOperatorConfigurator<T2, T> otherOperator)
        {
            _ = otherOperator ?? throw new ArgumentNullException(nameof(otherOperator));
            var edge = new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint());
            OutgoingEdges.Add(edge);
            otherOperator.IncomingEdges.Add(edge);
        }
    }
}
