using BlackSP.Core.OperatorShells;
using BlackSP.Infrastructure.Models;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration.Operators
{
    public class SourceOperatorConfigurator<TOperator, TOut> : ProducingOperatorConfiguratorBase<TOut>, ISourceOperatorConfigurator<TOperator, TOut>
        where TOperator : ISourceOperator<TOut>, new()
        where TOut : class, IEvent
    {

        public override Type ModuleType => typeof(SourceOperatorModule<SourceOperatorShell<TOut>, TOperator>);

        public override VertexType VertexType => VertexType.Source;
        public override ICollection<Edge> IncomingEdges => new List<Edge>();

        public SourceOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
