
namespace BlackSP.Infrastructure.Configuration.Operators
{
    public abstract class ProducingOperatorConfiguratorBase<T> : OperatorConfiguratorBase, IProducingOperatorConfigurator<T>
    {

        public ProducingOperatorConfiguratorBase(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }

        public void Append(IConsumingOperatorConfigurator<T> otherOperator)
        {
            OutgoingEdges.Add(new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint()));
        }

        public void Append<T2>(IConsumingOperatorConfigurator<T, T2> otherOperator)
        {
            OutgoingEdges.Add(new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint()));
        }

        public void Append<T2>(IConsumingOperatorConfigurator<T2, T> otherOperator)
        {
            OutgoingEdges.Add(new Edge(this, GetAvailableOutputEndpoint(), otherOperator, otherOperator.GetAvailableInputEndpoint()));
        }
    }
}
