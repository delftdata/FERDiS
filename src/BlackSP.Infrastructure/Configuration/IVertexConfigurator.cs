using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IVertexConfigurator
    {
        string VertexName { get; }
        ICollection<string> InstanceNames { get; }
        ICollection<Edge> OutgoingEdges { get; }
        ICollection<Edge> IncomingEdges { get; }

        VertexType VertexType { get; }
        Type ModuleType { get; }
        
        /// <summary>
        /// Transforms configurator to set of IVertexConfiguration
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

    public interface IOperatorConfigurator : IVertexConfigurator
    {   
    }

    /// <summary>
    /// Empty interface but highly relevant to perform compile-time typechecks against provided operator classes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConsumingOperatorConfigurator<T> : IOperatorConfigurator
    { }

    /// <summary>
    /// Empty interface but highly relevant to perform compile-time typechecks against provided operator classes
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public interface IConsumingOperatorConfigurator<T1, T2> : IConsumingOperatorConfigurator<T1>
    { }

    public interface IProducingOperatorConfigurator<T> : IOperatorConfigurator
    {
        void Append(IConsumingOperatorConfigurator<T> otherOperator);
        void Append<T2>(IConsumingOperatorConfigurator<T, T2> otherOperator);
        void Append<T2>(IConsumingOperatorConfigurator<T2, T> otherOperator);
    }

    public interface ISinkOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorConfigurator<TEvent>
        where TOperator : ISinkOperator<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface ISourceOperatorConfigurator<TOperator, TEvent> : IProducingOperatorConfigurator<TEvent>
        where TOperator : ISourceOperator<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface IMapOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorConfigurator<TIn>, IProducingOperatorConfigurator<TOut>
        where TOperator : IMapOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
    }

    public interface IFilterOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorConfigurator<TEvent>, IProducingOperatorConfigurator<TEvent>
        where TOperator : IFilterOperator<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface IAggregateOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorConfigurator<TIn>, IProducingOperatorConfigurator<TOut>
        where TOperator : IAggregateOperator<TIn, TOut>
        where TOut : class, IEvent
        where TIn : class, IEvent
    {
    }

    public interface IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> : IConsumingOperatorConfigurator<TIn1, TIn2>, IProducingOperatorConfigurator<TOut>
        where TOperator : IJoinOperator<TIn1, TIn2, TOut>
        where TOut : class, IEvent
        where TIn1 : class, IEvent
        where TIn2 : class, IEvent
    {
    }
}
