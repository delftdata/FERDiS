using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Infrastructure.Models;
using BlackSP.Simulator.Core;
using BlackSP.Simulator.Extensions;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Simulator.Configuration
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

        protected override Task<IContainer> BuildGraph()
        {   
            foreach (var edge in Configurators.SelectMany(c => c.OutgoingEdges))
            {
                foreach (var connection in edge.ToConnections())
                {
                    _connectionTable.RegisterConnection(connection);
                }
            }

            var graphConfig = GetVertexGraphConfiguration();
            
            foreach (var configurator in Configurators)
            {
                foreach (var vertexConf in configurator.ToConfigurations()) 
                {
                    var hostParameter = new HostConfiguration(configurator.ModuleType, graphConfig, vertexConf);
                    _identityTable.Add(vertexConf.InstanceName, hostParameter);
                }
            }

            var builder = new ContainerBuilder();
            //Note: invidivual Vertex instances register BlackSP types in their respective scopes
            builder.RegisterType<Vertex>();
            builder.RegisterType<VertexGraph>();
            builder.RegisterInstance(_connectionTable);
            builder.RegisterInstance(_identityTable);
            builder.RegisterLogger(new LoggerConfiguration().WriteTo.Console().CreateLogger());
            return Task.FromResult(builder.Build());
        }
    }
}
