using Autofac;
using BlackSP.Core.Controllers;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Models;
using BlackSP.CRA.Endpoints;
using BlackSP.Infrastructure;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private ControlLayerProcessController _controller;
        private IHostConfiguration _options;
        
        
        private CancellationTokenSource _ctSource;

        private Task _bspThread;

        public OperatorVertex()
        {
            _ctSource = new CancellationTokenSource();
        }
        
        ~OperatorVertex() {
            Dispose(false);
        }

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.WriteLine("Starting CRA Vertex initialization");
            _options = (vertexParameter as byte[])?.BinaryDeserialize() as IHostConfiguration ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(IHostConfiguration)}"); ;
            Console.WriteLine("Installing dependency container");
            
            InitializeIoCContainer();

            _controller = _vertexLifetimeScope.Resolve<ControlLayerProcessController>();
            _bspThread = _controller.StartProcess(_ctSource.Token);
            CreateEndpoints();
            
            Console.WriteLine("Vertex initialization completed");
            return Task.CompletedTask;
        }

        private void InitializeIoCContainer()
        {
            var container = new ContainerBuilder(); 
            //TODO: BlackSP Setup (with modules)
            container.RegisterType<VertexInputEndpoint>().As<IAsyncShardedVertexInputEndpoint>();
            container.RegisterType<VertexOutputEndpoint>().As<IAsyncShardedVertexOutputEndpoint>();
            
            _dependencyContainer = container.Build();

            Console.WriteLine("IoC setup completed");
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        private void CreateEndpoints()
        {
            var inputEndpointFactory = _vertexLifetimeScope.Resolve<InputEndpoint.Factory>();
            foreach (var endpointConfig in _options.VertexConfiguration.InputEndpoints)
            {
                var inputEndpoint = inputEndpointFactory.Invoke(endpointConfig.LocalEndpointName);
#pragma warning disable CA2000 // Dispose objects before losing scope
                AddAsyncInputEndpoint(endpointConfig.LocalEndpointName, new VertexInputEndpoint(inputEndpoint));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            var outputEndpointFactory = _vertexLifetimeScope.Resolve<OutputEndpoint.Factory>();
            foreach (var endpointConfig in _options.VertexConfiguration.OutputEndpoints)
            {
                var outputEndpoint = outputEndpointFactory.Invoke(endpointConfig.LocalEndpointName);
#pragma warning disable CA2000 // Dispose objects before losing scope
                AddAsyncOutputEndpoint(endpointConfig.LocalEndpointName, new VertexOutputEndpoint(outputEndpoint));
#pragma warning restore CA2000 // Dispose objects before losing scope
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
                _ctSource.Cancel();
                _bspThread.Wait();
                _dependencyContainer.Dispose();
                _bspThread.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
