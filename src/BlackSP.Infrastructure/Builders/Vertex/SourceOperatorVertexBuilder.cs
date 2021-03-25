using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.OperatorShells;
using System;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public class SourceOperatorVertexBuilder<TOperator, TOut> : ProducingOperatorVertexBuilderBase<TOut>
        where TOperator : ISourceOperator<TOut>
        where TOut : class, IEvent
    {
        
        public override Type ModuleType => typeof(SourceOperatorModule<SourceOperatorShell<TOut>, TOperator, TOut>);

        public override VertexType VertexType => VertexType.Source;

        public SourceOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
