using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Vertices
{

    public class JoinOperatorVertexBuilder<TOperator, TIn1, TIn2, TOut> : ProducingOperatorVertexBuilderBase<TOut>, IConsumingOperatorVertexBuilder<TIn1, TIn2>
        where TOperator : IJoinOperator<TIn1, TIn2, TOut>
        where TIn1 : class, IEvent
        where TIn2 : class, IEvent
        where TOut : class, IEvent
    {

        public override VertexType VertexType => VertexType.Operator;
        
        public override Type ModuleType => typeof(ReactiveOperatorModule<JoinOperatorShell<TIn1, TIn2, TOut>, TOperator>);

        public JoinOperatorVertexBuilder(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
