using BlackSP.Core.Endpoints;
using BlackSP.CRA.Vertices;
using BlackSP.Serialization.Serializers;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    public abstract class ProducingOperatorConfiguratorBase<T> : OperatorConfiguratorBase, IProducingOperatorConfigurator<T>
    {

        private readonly CRAClientLibrary _client;

        public ProducingOperatorConfiguratorBase(CRAClientLibrary craClient, string instanceName, string operatorName) : base(instanceName, operatorName)
        {
            _client = craClient;
        }

        public async Task RegisterCRAVertexAsync()
        {
            await _client.InstantiateVertexAsync(
                new[] { InstanceName },
                OperatorName,
                typeof(OperatorVertex).Name.ToLowerInvariant(),
                new VertexParameter(OperatorType, 
                                    OperatorConfigurationType, 
                                    InputEndpointNames.ToArray(), 
                                    typeof(InputEndpoint), 
                                    OutputEndpointNames.ToArray(), 
                                    typeof(OutputEndpoint), 
                                    typeof(ProtobufSerializer)),
                1
            );
        }

        public async Task AppendAsync(IConsumingOperatorConfigurator<T> otherOperator)
        {
            await _client.ConnectAsync(InstanceName, GetAvailableOutputEndpoint(), otherOperator.OperatorName, otherOperator.GetAvailableInputEndpoint());
        }

        public async Task AppendAsync<T2>(IConsumingOperatorConfigurator<T, T2> otherOperator)
        {
            await _client.ConnectAsync(InstanceName, GetAvailableOutputEndpoint(), otherOperator.OperatorName, otherOperator.GetAvailableInputEndpoint());
        }

        public async Task AppendAsync<T2>(IConsumingOperatorConfigurator<T2, T> otherOperator)
        {
            await _client.ConnectAsync(InstanceName, GetAvailableOutputEndpoint(), otherOperator.OperatorName, otherOperator.GetAvailableInputEndpoint());
        }
    }
}
