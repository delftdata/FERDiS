using BlackSP.Core.Operators;
using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Configuration
{
    public class SinkOperatorConfigurator<TOperator, TIn> : OperatorConfiguratorBase, ISinkOperatorConfigurator<TOperator, TIn>
        where TOperator : ISinkOperatorConfiguration<TIn>
        where TIn : class, IEvent
    {

        public override Type OperatorType => throw new NotImplementedException(); //TODO: fill when sink operator is implemented in core library
        public override Type OperatorConfigurationType => typeof(TOperator);
        public override ICollection<Edge> OutgoingEdges => new List<Edge>(); //always return empty list, sink has no outgoing edges ever

        public SinkOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }
    }
}
