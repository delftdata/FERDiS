using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class MapOperatorVertexBuilder<TOperator, TIn, TOut> : ProducingOperatorVertexBuilderBase<TOut>, IConsumingOperatorVertexBuilder<TIn>
        where TOperator : IMapOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        public override VertexType VertexType => VertexType.Operator;

        public override Type ModuleType => typeof(ReactiveOperatorModule<MapOperatorShell<TIn, TOut>, TOperator>);

        public MapOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {}
    }
}
