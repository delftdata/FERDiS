using BlackSP.Core.Operators;
using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    public interface IOperatorConfigurator
    {
        string InstanceName { get; }
        string OperatorName { get; }
        Type OperatorType { get; }
        Type OperatorConfigurationType { get; }

        string GetAvailableOutputEndpoint();
        string GetAvailableInputEndpoint();

        //do vertext register..? no, do in graphbuilder
        //do vertex create
        //do connection create
        //- get free input endpoint
        //- get free output endpoint
    }
    
    public interface IProducingOperatorConfigurator<T> : IOperatorConfigurator
    {
        Task AppendAsync(IConsumingOperatorConfigurator<T> otherOperator);
        Task AppendAsync<T2>(IConsumingOperatorConfigurator<T, T2> otherOperator);
        Task AppendAsync<T2>(IConsumingOperatorConfigurator<T2, T> otherOperator);

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

    public interface ISinkOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorConfigurator<TEvent>
        where TOperator : ISinkOperatorConfiguration<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface ISourceOperatorConfigurator<TOperator, TEvent> : IProducingOperatorConfigurator<TEvent>
        where TOperator : ISourceOperatorConfiguration<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface IMapOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorConfigurator<TIn>, IProducingOperatorConfigurator<TOut>
        where TOperator : IMapOperatorConfiguration<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
    }

    public interface IFilterOperatorConfigurator<TOperator, TEvent> : IConsumingOperatorConfigurator<TEvent>, IProducingOperatorConfigurator<TEvent>
        where TOperator : IFilterOperatorConfiguration<TEvent>
        where TEvent : class, IEvent
    {
    }

    public interface IAggregateOperatorConfigurator<TOperator, TIn, TOut> : IConsumingOperatorConfigurator<TIn>, IProducingOperatorConfigurator<TOut>
        where TOperator : IAggregateOperatorConfiguration<TIn, TOut>
        where TOut : class, IEvent
        where TIn : class, IEvent
    {
    }

    public interface IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> : IConsumingOperatorConfigurator<TIn1, TIn2>, IProducingOperatorConfigurator<TOut>
        where TOperator : IJoinOperatorConfiguration<TIn1, TIn2, TOut>
        where TOut : class, IEvent
        where TIn1 : class, IEvent
        where TIn2 : class, IEvent
    {
    }
}
