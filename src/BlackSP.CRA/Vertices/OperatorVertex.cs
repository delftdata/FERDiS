using Autofac;
using Autofac.Core;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel.Operators;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using System;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private IOperatorShell _bspOperator;
        private IHostConfiguration _options;
        
        public OperatorVertex()
        {
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
            
            _bspOperator = CreateOperatorShell();
            CreateEndpoints();

            //TODO: swap out for messageprocessor
            //_bspOperator.Start(DateTime.Now);
            
            Console.WriteLine("Vertex initialization completed");
            return Task.CompletedTask;
        }

        private void InitializeIoCContainer()
        {
            _dependencyContainer = new ContainerBuilder()
                .UseMessageProcessing()
                .RegisterBlackSPComponents(_options)
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexInputEndpoint>()
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexOutputEndpoint>()
                .Build();

            Console.WriteLine("IoC setup completed");
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        private IOperatorShell CreateOperatorShell()
        {
            Type operatorType = _options.OperatorShellType;
            return _vertexLifetimeScope.Resolve(operatorType) as IOperatorShell
                ?? throw new ArgumentException($"Resolved object with type {operatorType} could not be casted to {typeof(IOperatorShell)}");
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
                _dependencyContainer.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
