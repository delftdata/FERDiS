using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.CRA.Endpoints;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Core.Models;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private ControlMessageProcessor _controller;
        private ILogger _logger;
        
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
            var configuration = (vertexParameter as byte[])?.BinaryDeserialize() as IHostConfiguration ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(IHostConfiguration)}"); ;
            configuration.VertexConfiguration.SetCurrentShardId(shardId);
            InitializeIoC(configuration);
            _logger = _vertexLifetimeScope.Resolve<ILogger>();
            
            _logger.Information("Type registration completed succesfully, control layer start imminent");
            _controller = _vertexLifetimeScope.Resolve<ControlMessageProcessor>();
            _bspThread = _controller.StartProcess(_ctSource.Token);
            
            _logger.Information("Endpoint initialisation imminent");
            CreateEndpoints(configuration);

            _logger.Information("Vertex initialisation completed");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Configures Autofac to provide an Inversion of Control service according to the provided IHostConfiguration
        /// </summary>
        private void InitializeIoC(IHostConfiguration configuration)
        {
            var container = new ContainerBuilder(); 
            container.RegisterType<VertexInputEndpoint>().AsImplementedInterfaces().AsSelf();
            container.RegisterType<VertexOutputEndpoint>().AsImplementedInterfaces().AsSelf();
            container.ConfigureVertexHost(configuration); //configure vertexhost
            _dependencyContainer = container.Build();

            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        /// <summary>
        /// Creates and registers CRA endpoints + BlackSP endpoints
        /// </summary>
        /// <param name="configuration"></param>
        private void CreateEndpoints(IHostConfiguration configuration)
        {
            var vertexInputEndpointFactory = _vertexLifetimeScope.Resolve<VertexInputEndpoint.Factory>();
            foreach (var endpointConfig in configuration.VertexConfiguration.InputEndpoints)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                AddAsyncInputEndpoint(endpointConfig.LocalEndpointName, vertexInputEndpointFactory.Invoke(endpointConfig));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            var vertexOutputEndpointFactory = _vertexLifetimeScope.Resolve<VertexOutputEndpoint.Factory>();

            foreach (var endpointConfig in configuration.VertexConfiguration.OutputEndpoints)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                AddAsyncOutputEndpoint(endpointConfig.LocalEndpointName, vertexOutputEndpointFactory.Invoke(endpointConfig));
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
                _bspThread.Dispose();
                _ctSource.Dispose();
                _dependencyContainer.Dispose();
                _controller.Dispose();
                _vertexLifetimeScope.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
