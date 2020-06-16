using BlackSP.Core.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class AggregateOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IAggregateOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IAggregateOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        public override Type ModuleType => typeof(ReactiveOperatorModule<AggregateOperatorShell<TIn, TOut>, TOperator>);

        public AggregateOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
