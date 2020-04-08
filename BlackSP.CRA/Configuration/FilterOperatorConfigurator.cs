using BlackSP.Core.Operators;
using BlackSP.Core.Operators.Concrete;
using BlackSP.Kernel.Events;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    public class FilterOperatorConfigurator<TOperator, TEvent> : ProducingOperatorConfiguratorBase<TEvent>, IFilterOperatorConfigurator<TOperator, TEvent>
        where TOperator : IFilterOperatorConfiguration<TEvent>
        where TEvent : class, IEvent
    {

        public override Type OperatorType => typeof(FilterOperator<TEvent>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public FilterOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
