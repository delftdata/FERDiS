using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.Core.OperatorSockets;
using BlackSP.CRA.DI;
using BlackSP.CRA.Endpoints;
using BlackSP.CRA.Events;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using BlackSP.Serialization;
using CRA.ClientLibrary;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private IOperatorSocket _bspOperator;
        private IVertexParameter _options;
        
        public OperatorVertex()
        {
        }
        
        ~OperatorVertex() {
            Dispose(false);
        }

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.WriteLine("Starting CRA Vertex initialization");
            _options = vertexParameter as IVertexParameter ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(IVertexParameter)}"); ;
            
            Console.WriteLine("Installing dependency container");
            InitializeIoCContainer();
            
            _bspOperator = ResolveOperator();
            SetupInputEndpoints();
            SetupOutputEndpoints();
                   
            _bspOperator.Start(DateTime.Now);
            
            Console.WriteLine("Vertex initialization completed");
            return Task.CompletedTask;
        }

        private void InitializeIoCContainer()
        {
            _dependencyContainer = new IoC(_options)
                .RegisterBlackSPComponents()
                .RegisterCRAComponents()
                .BuildContainer();
            //TODO: register logger?

            Console.WriteLine("IoC setup completed");
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        private IOperatorSocket ResolveOperator()
        {
            Type operatorType = _options.OperatorType;
            return _vertexLifetimeScope.Resolve(operatorType) as IOperatorSocket
                ?? throw new ArgumentException($"Resolved object with type {operatorType} could not be casted to {typeof(IOperatorSocket)}");
        }

        private void SetupInputEndpoints()
        {
            foreach (string endpointName in _options.InputEndpointNames)
            {
                AddAsyncInputEndpoint(endpointName, _vertexLifetimeScope.Resolve<IAsyncShardedVertexInputEndpoint>());
            }
        }

        private void SetupOutputEndpoints()
        {
            foreach (string endpointName in _options.OutputEndpointNames)
            {
                AddAsyncOutputEndpoint(endpointName, _vertexLifetimeScope.Resolve<IAsyncShardedVertexOutputEndpoint>());
            }
        }

        #region Dispose support
        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        private new void Dispose(bool disposing)
        {
            if(disposing)
            {
                _dependencyContainer.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
