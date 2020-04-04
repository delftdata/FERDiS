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
    public class MapOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IMapOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IMapOperatorConfiguration<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        public override Type OperatorType => typeof(MapOperator<TIn, TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);
        public MapOperatorConfigurator(CRAClientLibrary craClient, string instanceName, string operatorName) : base(craClient, instanceName, operatorName)
        {}
    }
}
