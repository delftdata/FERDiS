using BlackSP.Core.OperatorShells;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration.Operators
{
    public class SinkOperatorConfigurator<TOperator, TIn> : OperatorConfiguratorBase, ISinkOperatorConfigurator<TOperator, TIn>
        where TOperator : ISinkOperator<TIn>
        where TIn : class, IEvent
    {

        public override Type OperatorType => typeof(SinkOperatorShell<TIn>);
        public override Type OperatorConfigurationType => typeof(TOperator);
        public override ICollection<Edge> OutgoingEdges => new List<Edge>(); //always return empty list, sink has no outgoing edges ever

        public SinkOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }
    }
}
