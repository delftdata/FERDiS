using BlackSP.Core.Operators;
using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Configuration
{
    public interface IOperatorGraphBuilder
    {
        ISinkOperatorConfigurator<TOperator, TOut> AddSink<TOperator, TOut>()
            where TOperator : ISinkOperatorConfiguration<TOut>, new()
            where TOut : class, IEvent;

        ISourceOperatorConfigurator<TOperator, TIn> AddSource<TOperator, TIn>()
            where TOperator : ISourceOperatorConfiguration<TIn>, new()
            where TIn : class, IEvent;

        IMapOperatorConfigurator<TOperator, TIn, TOut> AddMap<TOperator, TIn, TOut>()
            where TOperator : IMapOperatorConfiguration<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IFilterOperatorConfigurator<TOperator, TEvent> AddFilter<TOperator, TEvent>()
            where TOperator : IFilterOperatorConfiguration<TEvent>, new()
            where TEvent : class, IEvent;

        IAggregateOperatorConfigurator<TOperator, TIn, TOut> AddAggregate<TOperator, TIn, TOut>()
            where TOperator : IAggregateOperatorConfiguration<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> AddJoin<TOperator, TIn1, TIn2, TOut>()
            where TOperator : IJoinOperatorConfiguration<TIn1, TIn2, TOut>, new()
            where TIn1 : class, IEvent
            where TIn2 : class, IEvent
            where TOut : class, IEvent;
    }
}
