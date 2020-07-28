using BlackSP.CRA.Extensions;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Infrastructure.Models;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    class CRAOperatorGraphBuilder : OperatorVertexGraphBuilderBase
    {
        private readonly KubernetesDeploymentUtility _k8sDeploymentUtil;
        private readonly CRAClientLibrary _craClient;

        public CRAOperatorGraphBuilder(KubernetesDeploymentUtility k8sUtil, CRAClientLibrary craClient) : base()
        {
            _k8sDeploymentUtil = k8sUtil ?? throw new ArgumentNullException(nameof(k8sUtil));
            _craClient = craClient ?? throw new ArgumentNullException(nameof(craClient));
        }

        protected override async Task<object> BuildGraph()
        {
            await RegisterGraphWithCRA().ConfigureAwait(false);
            _k8sDeploymentUtil.With(VertexBuilders).WriteDeploymentYaml();
            _k8sDeploymentUtil.PrintUsage();
            return null;
        }

        protected async Task RegisterGraphWithCRA()
        {
            await _craClient.ResetClusterAsync().ConfigureAwait(false);

            string craVertexName = typeof(OperatorVertex).Name.ToLowerInvariant();
            await _craClient.DefineVertexAsync(craVertexName, () => new OperatorVertex()).ConfigureAwait(false);

            foreach (var builder in VertexBuilders)
            {
                await RegisterCRAVertexAsync(builder, craVertexName).ConfigureAwait(false);
            }

            foreach (var edge in VertexBuilders.SelectMany(c => c.OutgoingEdges))
            {
                await _craClient.ConnectAsync(edge.FromVertex.VertexName, edge.FromEndpoint, edge.ToVertex.VertexName, edge.ToEndpoint).ConfigureAwait(false);
            }
        } 

        private async Task RegisterCRAVertexAsync(IVertexBuilder target, string vertexDefinition)
        {
            var i = 0;
            foreach (var config in target.ToConfigurations())
            {
                var hostParameter = new HostConfiguration(target.ModuleType, GetVertexGraphConfiguration(), config);
                await _craClient.InstantiateVertexAsync(
                    config.InstanceName,
                    target.VertexName,
                    vertexDefinition,
                    hostParameter.BinarySerialize(),
                    i
                ).ConfigureAwait(true);
                
                i++;
            }            
        }
    }
}
