using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IVertexBuilder
    {
        string VertexName { get; }
        ICollection<string> InstanceNames { get; }
        ICollection<Edge> OutgoingEdges { get; }
        ICollection<Edge> IncomingEdges { get; }

        VertexType VertexType { get; }
        Type ModuleType { get; }
        
        /// <summary>
        /// Builds a set of IVertexConfiguration's representing the configurations for each shard of the Vertex
        /// </summary>
        /// <returns></returns>
        IEnumerable<IVertexConfiguration> ToConfigurations();

        /// <summary>
        /// Returns a new unique identifier for an output endpoint, gets persisted in OutputEndpointNames property
        /// </summary>
        /// <returns></returns>
        string GetAvailableOutputEndpoint();

        /// <summary>
        /// Returns a new unique identifier for an input endpoint, gets persisted in InputEndpointNames property
        /// </summary>
        /// <returns></returns>
        string GetAvailableInputEndpoint();
    }

    public interface IOperatorVertexBuilder : IVertexBuilder
    {   
    }

    /// <summary>
    /// Empty interface but highly relevant to perform compile-time typechecks against provided operator classes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConsumingOperatorVertexBuilder<T> : IOperatorVertexBuilder
    { }

    /// <summary>
    /// Empty interface but highly relevant to perform compile-time typechecks against provided operator classes
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public interface IConsumingOperatorVertexBuilder<T1, T2> : IConsumingOperatorVertexBuilder<T1>
    { }

    public interface IProducingOperatorVertexBuilder<T> : IOperatorVertexBuilder
    {
        void Append(IConsumingOperatorVertexBuilder<T> otherOperator);
        void Append<T2>(IConsumingOperatorVertexBuilder<T, T2> otherOperator);
        void Append<T2>(IConsumingOperatorVertexBuilder<T2, T> otherOperator);
    }

/*
    public interface ISinkOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorVertexBuilder<TEvent>
        where TOperator : ISinkOperator<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface ISourceOperatorConfigurator<TOperator, TEvent> : IProducingOperatorVertexBuilder<TEvent>
        where TOperator : ISourceOperator<TEvent>
        where TEvent : class, IEvent
    {
    }


    public interface IMapOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorVertexBuilder<TIn>, IProducingOperatorVertexBuilder<TOut>
        where TOperator : IMapOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
    }

    public interface IFilterOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorVertexBuilder<TEvent>, IProducingOperatorVertexBuilder<TEvent>
        where TOperator : IFilterOperator<TEvent>
        where TEvent : class, IEvent
    {
    }

    
    public interface IAggregateOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorVertexBuilder<TIn>, IProducingOperatorVertexBuilder<TOut>
        where TOperator : IAggregateOperator<TIn, TOut>
        where TOut : class, IEvent
        where TIn : class, IEvent
    {
    }
 

    public interface IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> : IConsumingOperatorVertexBuilder<TIn1, TIn2>, IProducingOperatorVertexBuilder<TOut>
        where TOperator : IJoinOperator<TIn1, TIn2, TOut>
        where TOut : class, IEvent
        where TIn1 : class, IEvent
        where TIn2 : class, IEvent
    {
    }   
*/
}
