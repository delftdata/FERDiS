using BlackSP.Core.Operators;
using BlackSP.Kernel.Events;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Configuration.Operators
{
    public class SourceOperatorConfigurator<TOperator, TOut> : ProducingOperatorConfiguratorBase<TOut>, ISourceOperatorConfigurator<TOperator, TOut>
        where TOperator : ISourceOperatorConfiguration<TOut>, new()
        where TOut : class, IEvent
    {
        public override Type OperatorType => throw new NotImplementedException(); //TODO: fill when SourceOperator exists in core library
        public override Type OperatorConfigurationType => typeof(TOperator);

        public SourceOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
