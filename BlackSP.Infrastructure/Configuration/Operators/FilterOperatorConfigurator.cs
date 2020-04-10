using BlackSP.Core.OperatorSockets;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Operators
{
    public class FilterOperatorConfigurator<TOperator, TEvent> : ProducingOperatorConfiguratorBase<TEvent>, IFilterOperatorConfigurator<TOperator, TEvent>
        where TOperator : IFilterOperator<TEvent>
        where TEvent : class, IEvent
    {

        public override Type OperatorType => typeof(FilterOperatorSocket<TEvent>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public FilterOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
