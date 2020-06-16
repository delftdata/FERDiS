using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.InMemory.Core;
using BlackSP.InMemory.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackSP.Infrastructure.Models;

namespace BlackSP.InMemory.Configuration
{
    class InMemoryOperatorGraphBuilder : OperatorGraphBuilderBase<IContainer>
    {

        private readonly ConnectionTable _connectionTable;
        private readonly IdentityTable _identityTable;
        public InMemoryOperatorGraphBuilder(ConnectionTable connectionTable, IdentityTable identityTable) : base()
        {
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
        }

        public override Task<IContainer> BuildGraph()
        {
            //failure & restart functionality?
   
            foreach (var edge in Configurators.SelectMany(c => c.OutgoingEdges))
            {
                foreach (var connection in edge.ToConnections())
                {
                    _connectionTable.RegisterConnection(connection);
                }
            }

            foreach (var configurator in Configurators)
            {
                foreach (var vertexConf in configurator.ToConfigurations()) 
                {
                    var hostParameter = new HostConfiguration(configurator.ModuleType, vertexConf);
                    _identityTable.Add(vertexConf.InstanceName, hostParameter);
                }
            }
            var builder = new ContainerBuilder();
            //Note: invidivual Vertex instances register BlackSP types in their respective scopes
            builder.RegisterType<Vertex>();
            builder.RegisterType<VertexGraph>();
            //builder.RegisterType<OperatorShellHost>();
            builder.RegisterType<InputEndpointHost>();
            builder.RegisterType<OutputEndpointHost>();
            builder.RegisterInstance(_connectionTable);//.AsImplementedInterfaces();
            builder.RegisterInstance(_identityTable);//.AsImplementedInterfaces();

            return Task.FromResult(builder.Build());
        }
    }
}
