using Autofac;
using Autofac.Core;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel;
using BlackSP.Kernel.Operators;
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
        private IMessageProcessor _processor;
        private IHostConfiguration _options;
        private CancellationTokenSource _ctSource;

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
            _vertexLifetimeScope.StartMessageProcessorSubsystems(_ctSource.Token);
            CreateEndpoints();
            
            Console.WriteLine("Vertex initialization completed");
            return Task.CompletedTask;
        }

        private void InitializeIoCContainer()
        {
            _dependencyContainer = new ContainerBuilder()
                .UseMessageProcessing()
                .UseOperatorMiddleware(_options)
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexInputEndpoint>()
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexOutputEndpoint>()
                .Build();

            Console.WriteLine("IoC setup completed");
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        private void CreateEndpoints()
        {
            foreach (var endpointConfig in _options.VertexConfiguration.InputEndpoints)
            {
                AddAsyncInputEndpoint(endpointConfig.LocalEndpointName, _vertexLifetimeScope.Resolve<IAsyncShardedVertexInputEndpoint>());
            }
            foreach (var endpointConfig in _options.VertexConfiguration.OutputEndpoints)
            {
                AddAsyncOutputEndpoint(endpointConfig.LocalEndpointName, _vertexLifetimeScope.Resolve<IAsyncShardedVertexOutputEndpoint>());
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
                _dependencyContainer.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
