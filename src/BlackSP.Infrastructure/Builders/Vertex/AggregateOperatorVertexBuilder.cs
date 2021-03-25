using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Configuration;
using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public class AggregateOperatorVertexBuilder<TOperator, TIn, TOut> : ProducingOperatorVertexBuilderBase<TOut>, IConsumingOperatorVertexBuilder<TIn>
        where TOperator : IAggregateOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        public override VertexType VertexType => VertexType.Operator;

        public override Type ModuleType => typeof(ReactiveOperatorModule<AggregateOperatorShell<TIn, TOut>, TOperator>);

        public AggregateOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
