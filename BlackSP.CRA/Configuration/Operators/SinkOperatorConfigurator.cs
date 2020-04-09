using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.CRA.Configuration.Operators
{
    public class SinkOperatorConfigurator<TOperator, TIn> : OperatorConfiguratorBase, ISinkOperatorConfigurator<TOperator, TIn>
        where TOperator : ISinkOperator<TIn>
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
