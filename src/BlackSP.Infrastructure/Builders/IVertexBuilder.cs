using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Builders
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
        IVertexConfiguration GetVertexConfiguration();

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
}
