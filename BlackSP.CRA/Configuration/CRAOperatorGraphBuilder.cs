using BlackSP.Core.Endpoints;
using BlackSP.CRA.Extensions;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Infrastructure.Configuration.Operators;
using BlackSP.Infrastructure.IoC;
using BlackSP.Serialization.Serializers;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    class CRAOperatorGraphBuilder : OperatorGraphBuilderBase
    {
        private readonly KubernetesDeploymentUtility _k8sDeploymentUtil;
        private readonly CRAClientLibrary _craClient;

        public CRAOperatorGraphBuilder(KubernetesDeploymentUtility k8sUtil, CRAClientLibrary craClient) : base()
        {
            _k8sDeploymentUtil = k8sUtil ?? throw new ArgumentNullException(nameof(k8sUtil));
            _craClient = craClient ?? throw new ArgumentNullException(nameof(craClient));
        }

        public override async Task<object> BuildGraph()
        {
            await RegisterGraphWithCRA();
            _k8sDeploymentUtil.With(Configurators).WriteDeploymentYaml();
            _k8sDeploymentUtil.PrintUsage();
            return null;
        }

        protected async Task RegisterGraphWithCRA()
        {
            await _craClient.ResetClusterAsync();

            string craVertexName = typeof(OperatorVertex).Name.ToLowerInvariant();
            await _craClient.DefineVertexAsync(craVertexName, () => new OperatorVertex());

            foreach (var configurator in Configurators)
            {
                await RegisterCRAVertexAsync(configurator, craVertexName);
            }

            foreach (var edge in Configurators.SelectMany(c => c.OutgoingEdges))
            {
                await _craClient.ConnectAsync(edge.FromOperator.OperatorName, edge.FromEndpoint, edge.ToOperator.OperatorName, edge.ToEndpoint);
            }
        } 

        private async Task RegisterCRAVertexAsync(IOperatorConfigurator target, string vertexDefinition)
        {
            var vertexParameter = new HostParameter(
                target.OperatorType,
                target.OperatorConfigurationType,
                target.InputEndpointNames.ToArray(),
                typeof(InputEndpoint),
                target.OutputEndpointNames.ToArray(),
                typeof(OutputEndpoint),
                typeof(ProtobufSerializer)
            );

            await _craClient.InstantiateVertexAsync(
                target.InstanceNames,
                target.OperatorName,
                vertexDefinition,
                vertexParameter,
                1
            );
        }
    }
}
