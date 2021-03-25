using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.OperatorShells;
using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public class FilterOperatorVertexBuilder<TOperator, TEvent> : ProducingOperatorVertexBuilderBase<TEvent>, IConsumingOperatorVertexBuilder<TEvent>
        where TOperator : IFilterOperator<TEvent>
        where TEvent : class, IEvent
    {

        public override VertexType VertexType => VertexType.Operator;

        public override Type ModuleType => typeof(ReactiveOperatorModule<FilterOperatorShell<TEvent>, TOperator>);

        public FilterOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
