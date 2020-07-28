﻿using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Models;
using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration.Vertices
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