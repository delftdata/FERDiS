using BlackSP.Core.OperatorShells;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Operators
{
    public class AggregateOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IAggregateOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IAggregateOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        public override Type OperatorType => typeof(AggregateOperatorShell<TIn, TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public AggregateOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
