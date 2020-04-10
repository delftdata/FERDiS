using BlackSP.Core.Endpoints;
using BlackSP.Infrastructure.Configuration;
using BlackSP.InMemory.Core;
using BlackSP.InMemory.Extensions;
using BlackSP.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Configuration
{
    class InMemoryOperatorGraphBuilder : OperatorGraphBuilderBase<VertexGraph>
    {

        private readonly ConnectionTable _connectionTable;

        public InMemoryOperatorGraphBuilder(ConnectionTable connectionTable) : base()
        {
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
        }

        public override Task<VertexGraph> BuildGraph()
        {
            //failure & restart functionality?

            //introduce vertex type
            //- Start(hostparameter, connectiontable)
            //-- create IoC scope!
            //--- hostparameter with types (go generics?)
            //-- get connectiontable
            //-- instantiate shell hosts
            //-- start host
            //- die() forces threads to die

            //operatorshellhost
            //- instantiated through ioc
            //- ioperator injected
            //- connectionhost injected
            //- start(at)
            //-- start operator
            //-- start connectionhost

            //connectionhost
            //- instantiated through ioc
            //- input and output endpoint injected
            //- connectiontable injected
            //- start()
            //-- fetches streams from connectiontable
            //-- invokes ingress/egress for each stream
            //-- joins all threads

            //connection
            //- represents connection from shard to shard

            //connectiontable
            //- singleton global ioc scope?
            //- considers shards
            //- getIncomingConnections(operatorName, instanceName, endpointName)
            //-- returns collection
            //- getOutGoingConnections(operatorName, instanceName, endpointName)
            //-- returns collection
            
            foreach (var configurator in Configurators)
            {
                //configurator.InstanceNames.Length; //number local of shards

                var vertexParameter = new HostParameter(
                    configurator.OperatorType,
                    configurator.OperatorConfigurationType,
                    configurator.InputEndpointNames.ToArray(),
                    typeof(InputEndpoint),
                    configurator.OutputEndpointNames.ToArray(),
                    typeof(OutputEndpoint),
                    typeof(ProtobufSerializer)
                );

                foreach (var edge in configurator.OutgoingEdges)
                {
                    //edge.ToOperator.InstanceNames.Length; //number of remote shards
                    foreach(var connection in edge.ToConnections())
                    {
                        _connectionTable.RegisterConnection(connection);
                    }
                }
            }

            return Task.FromResult<VertexGraph>(null);
        }
    }
}
