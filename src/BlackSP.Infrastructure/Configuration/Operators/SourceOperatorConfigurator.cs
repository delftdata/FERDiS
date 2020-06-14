using BlackSP.Core.OperatorShells;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Operators
{
    public class SourceOperatorConfigurator<TOperator, TOut> : ProducingOperatorConfiguratorBase<TOut>, ISourceOperatorConfigurator<TOperator, TOut>
        where TOperator : ISourceOperator<TOut>, new()
        where TOut : class, IEvent
    {
        public override Type OperatorType => typeof(SourceOperatorShell<TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public SourceOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
