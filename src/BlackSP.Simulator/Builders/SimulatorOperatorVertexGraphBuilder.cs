using Autofac;
using AutofacSerilogIntegration;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Models;
using BlackSP.Serialization.Extensions;
using BlackSP.Simulator.Configuration;
using BlackSP.Simulator.Core;
using BlackSP.Simulator.Extensions;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Simulator.Builders
{
    class SimulatorOperatorVertexGraphBuilder : OperatorVertexGraphBuilderBase
    {

        private readonly ConnectionTable _connectionTable;
        private readonly IdentityTable _identityTable;
        public SimulatorOperatorVertexGraphBuilder(ConnectionTable connectionTable, IdentityTable identityTable) : base()
        {
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
        }

        protected override Task<IApplication> BuildGraph()
        {   
            foreach (var edge in VertexBuilders.SelectMany(c => c.OutgoingEdges))
            {
                foreach (var connection in edge.ToConnections())
                {
                    _connectionTable.RegisterConnection(connection);
                }
            }

            var graphConfig = GetVertexGraphConfiguration();
            
            foreach (var vbuilder in VertexBuilders)
            {
                var vertexConf = vbuilder.GetVertexConfiguration();
                var logConfig = LogConfiguration;
                
                for(int i = 0; i < vertexConf.InstanceNames.Count(); i++)
                {
                    var shardId = i;
                    var vertexConfCopy = vertexConf.BinarySerialize().BinaryDeserialize() as IVertexConfiguration;
                    vertexConfCopy.SetCurrentShardId(shardId);
                    var hostParameter = new HostConfiguration(vbuilder.ModuleType, graphConfig, vertexConfCopy, logConfig);
                    _identityTable.Add(vertexConfCopy.InstanceName, hostParameter);
                }

            }

            var builder = new ContainerBuilder();
            //Note: invidivual Vertex instances register BlackSP types in their respective scopes
            builder.RegisterType<Vertex>();
            builder.RegisterType<VertexGraph>();
            builder.RegisterInstance(_connectionTable);
            builder.RegisterInstance(_identityTable);
            //Note: this is only a top level logger (used in types defined here), BlackSP types use their respective log configurations
            builder.RegisterLogger(new LoggerConfiguration().WriteTo.Console().CreateLogger()); 

            IApplication runnable = new SimulatorRunnable(builder.Build());
            return Task.FromResult(runnable);
        }
    }
}
