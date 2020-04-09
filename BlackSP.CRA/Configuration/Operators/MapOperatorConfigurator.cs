﻿using BlackSP.Core.OperatorSockets;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.CRA.Configuration.Operators
{
    public class MapOperatorConfigurator<TOperator, TIn, TOut> : ProducingOperatorConfiguratorBase<TOut>, IMapOperatorConfigurator<TOperator, TIn, TOut>
        where TOperator : IMapOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        public override Type OperatorType => typeof(MapOperatorSocket<TIn, TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);
        public MapOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {}
    }
}