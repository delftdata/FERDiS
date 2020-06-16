using BlackSP.Core.OperatorShells;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class MapOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IMapOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IMapOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        public override Type ModuleType => typeof(ReactiveOperatorModule<MapOperatorShell<TIn, TOut>, TOperator>);

        public MapOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {}
    }
}
