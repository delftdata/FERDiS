using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.OperatorShells;
using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public class SinkOperatorVertexBuilder<TOperator, TIn> : VertexBuilderBase, IConsumingOperatorVertexBuilder<TIn>
        where TOperator : ISinkOperator<TIn>
        where TIn : class, IEvent
    {
        
        public override VertexType VertexType => VertexType.Operator;

        public override Type ModuleType => typeof(ReactiveOperatorModule<SinkOperatorShell<TIn>, TOperator>);

        public SinkOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }
    }
}
