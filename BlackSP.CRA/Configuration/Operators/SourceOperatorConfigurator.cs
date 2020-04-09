using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.CRA.Configuration.Operators
{
    public class SourceOperatorConfigurator<TOperator, TOut> : ProducingOperatorConfiguratorBase<TOut>, ISourceOperatorConfigurator<TOperator, TOut>
        where TOperator : ISourceOperator<TOut>, new()
        where TOut : class, IEvent
    {
        public override Type OperatorType => throw new NotImplementedException(); //TODO: fill when SourceOperator exists in core library
        public override Type OperatorConfigurationType => typeof(TOperator);

        public SourceOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
