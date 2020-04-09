using BlackSP.Core.Endpoints;
using BlackSP.CRA.Vertices;
using BlackSP.Serialization.Serializers;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration.Operators
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
