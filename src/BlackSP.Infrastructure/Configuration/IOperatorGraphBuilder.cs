using BlackSP.Infrastructure.Configuration.Vertices;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IOperatorGraphBuilder
    {

        /// <summary>
        /// Registers a sink operator with the operator graph
        /// </summary>
        /// <typeparam name="TOperator"></typeparam>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="shardCount"></param>
        /// <returns></returns>
        ISinkOperatorConfigurator<TOperator, TIn> AddSink<TOperator, TIn>(int shardCount)
            where TOperator : ISinkOperator<TIn>, new()
            where TIn : class, IEvent;

        ISourceOperatorConfigurator<TOperator, TOut> AddSource<TOperator, TOut>(int shardCount)
            where TOperator : ISourceOperator<TOut>, new()
            where TOut : class, IEvent;

        IMapOperatorConfigurator<TOperator, TIn, TOut> AddMap<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IMapOperator<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IFilterOperatorConfigurator<TOperator, TEvent> AddFilter<TOperator, TEvent>(int shardCount)
            where TOperator : IFilterOperator<TEvent>, new()
            where TEvent : class, IEvent;

        IAggregateOperatorConfigurator<TOperator, TIn, TOut> AddAggregate<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IAggregateOperator<TIn, TOut>, new()
            where TIn : class, IEvent
            where TOut : class, IEvent;

        IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
            where TOperator : IJoinOperator<TIn1, TIn2, TOut>, new()
            where TIn1 : class, IEvent
            where TIn2 : class, IEvent
            where TOut : class, IEvent;
    }
}
