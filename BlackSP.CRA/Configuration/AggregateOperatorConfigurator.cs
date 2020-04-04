using BlackSP.Core.Operators;
using BlackSP.Core.Operators.Concrete;
using BlackSP.Kernel.Events;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Configuration
{

    public class AggregateOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IAggregateOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IAggregateOperatorConfiguration<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        public override Type OperatorType => typeof(AggregateOperator<TIn, TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public AggregateOperatorConfigurator(CRAClientLibrary craClient, string instanceName, string operatorName) : base(craClient, instanceName, operatorName)
        { }
    }
}
