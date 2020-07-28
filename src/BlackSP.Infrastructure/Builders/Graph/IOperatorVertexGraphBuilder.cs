using BlackSP.Infrastructure.Builders.Vertex;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Builders.Graph
{
    /// <summary>
    /// Interface for building a graph of operator vertices by adding different types of operators
    /// </summary>
    public interface IOperatorVertexGraphBuilder
    {

        /// <summary>
        /// Registers a sink operator with the operator graph
        /// </summary>
        /// <typeparam name="TOperator"></typeparam>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="shardCount"></param>
        /// <returns></returns>
        SinkOperatorVertexBuilder<TOperator, TIn> AddSink<TOperator, TIn>(int shardCount)
            where TOperator : ISinkOperator<TIn>
            where TIn : class, IEvent;

        SourceOperatorVertexBuilder<TOperator, TOut> AddSource<TOperator, TOut>(int shardCount)
            where TOperator : ISourceOperator<TOut>
            where TOut : class, IEvent;

        MapOperatorVertexBuilder<TOperator, TIn, TOut> AddMap<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IMapOperator<TIn, TOut>
            where TIn : class, IEvent
            where TOut : class, IEvent;

        FilterOperatorVertexBuilder<TOperator, TEvent> AddFilter<TOperator, TEvent>(int shardCount)
            where TOperator : IFilterOperator<TEvent>
            where TEvent : class, IEvent;

        AggregateOperatorVertexBuilder<TOperator, TIn, TOut> AddAggregate<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IAggregateOperator<TIn, TOut>
            where TIn : class, IEvent
            where TOut : class, IEvent;

        JoinOperatorVertexBuilder<TOperator, TIn1, TIn2, TOut> AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
            where TOperator : IJoinOperator<TIn1, TIn2, TOut>
            where TIn1 : class, IEvent
            where TIn2 : class, IEvent
            where TOut : class, IEvent;
    }
}
