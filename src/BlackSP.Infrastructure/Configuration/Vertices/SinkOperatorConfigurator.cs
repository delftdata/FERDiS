﻿using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Models;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class SinkOperatorConfigurator<TOperator, TIn> : OperatorConfiguratorBase, ISinkOperatorConfigurator<TOperator, TIn>
        where TOperator : ISinkOperator<TIn>
        where TIn : class, IEvent
    {
        public override Type ModuleType => typeof(ReactiveOperatorModule<SinkOperatorShell<TIn>, TOperator>);

        //public override ICollection<Edge> OutgoingEdges => new List<Edge>(); //always return empty list, sink has no outgoing edges ever

        public SinkOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }
    }
}