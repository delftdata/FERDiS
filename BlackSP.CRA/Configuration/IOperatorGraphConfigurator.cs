using BlackSP.Core.Operators;
using BlackSP.CRA.Configuration.Operators;
using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    public interface IOperatorGraphConfigurator
    {
        ISinkOperatorConfigurator<TOperator, TIn> AddSink<TOperator, TIn>(int shardCount)
            where TOperator : ISinkOperatorConfiguration<TIn>, new()
            where TIn : class, IEvent;

        ISourceOperatorConfigurator<TOperator, TOut> AddSource<TOperator, TOut>(int shardCount)
            where TOperator : ISourceOperatorConfiguration<TOut>, new()
            where TOut : class, IEvent;

        IMapOperatorConfigurator<TOperator, TIn, TOut> AddMap<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IMapOperatorConfiguration<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IFilterOperatorConfigurator<TOperator, TEvent> AddFilter<TOperator, TEvent>(int shardCount)
            where TOperator : IFilterOperatorConfiguration<TEvent>, new()
            where TEvent : class, IEvent;

        IAggregateOperatorConfigurator<TOperator, TIn, TOut> AddAggregate<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IAggregateOperatorConfiguration<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
            where TOperator : IJoinOperatorConfiguration<TIn1, TIn2, TOut>, new()
            where TIn1 : class, IEvent
            where TIn2 : class, IEvent
            where TOut : class, IEvent;
    }
}
