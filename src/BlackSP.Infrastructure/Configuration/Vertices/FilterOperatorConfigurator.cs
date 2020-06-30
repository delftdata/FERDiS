using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class FilterOperatorConfigurator<TOperator, TEvent> : ProducingOperatorConfiguratorBase<TEvent>, IFilterOperatorConfigurator<TOperator, TEvent>
        where TOperator : IFilterOperator<TEvent>
        where TEvent : class, IEvent
    {
        public override Type ModuleType => typeof(ReactiveOperatorModule<FilterOperatorShell<TEvent>, TOperator>);

        public FilterOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
