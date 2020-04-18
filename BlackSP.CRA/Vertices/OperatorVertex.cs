using Autofac;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel.Operators;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private IOperatorSocket _bspOperator;
        private IHostParameter _options;
        
        public OperatorVertex()
        {
        }
        
        ~OperatorVertex() {
            Dispose(false);
        }

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.WriteLine("Starting CRA Vertex initialization");

            //AppDomain.CurrentDomain.LoadAllAvailableAssemblies(Assembly.GetEntryAssembly()); //ensure dependency types are loaded (otherwise they remain invisible)


            _options = (vertexParameter as byte[])?.BinaryDeserialize() as IHostParameter ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(IHostParameter)}"); ;
            
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

            _dependencyContainer = new ContainerBuilder().RegisterBlackSPComponents(_options)
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexInputEndpoint>()
                .RegisterAllConcreteClassesOfType<IAsyncShardedVertexOutputEndpoint>()
                .Build();

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
