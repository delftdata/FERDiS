using BlackSP.CRA.Extensions;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Graph;
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

        protected override async Task<IApplication> BuildGraph()
        {
            await RegisterGraphWithCRA().ConfigureAwait(false);
            _k8sDeploymentUtil.With(VertexBuilders).WriteDeploymentYaml();
            _k8sDeploymentUtil.PrintUsage();
            return new CRAApplication();
        }

        protected async Task RegisterGraphWithCRA()
        {
            await _craClient.ResetClusterAsync().ConfigureAwait(false);

            string craVertexDefinition = typeof(OperatorVertex).Name.ToLowerInvariant();
            await _craClient.DefineVertexAsync(craVertexDefinition, () => new OperatorVertex()).ConfigureAwait(false);

            foreach (var builder in VertexBuilders)
            {
                await RegisterCRAVertexAsync(builder, craVertexDefinition).ConfigureAwait(false);
            }

            foreach (var edge in VertexBuilders.SelectMany(c => c.OutgoingEdges))
            {
                await _craClient.ConnectAsync(edge.FromVertex.VertexName, edge.FromEndpoint, edge.ToVertex.VertexName, edge.ToEndpoint).ConfigureAwait(false);
            }
        } 

        private async Task RegisterCRAVertexAsync(IVertexBuilder target, string vertexDefinition)
        {
            //var i = 0;

            var vertexConfig = target.GetVertexConfiguration();
            var hostConfig = new HostConfiguration(target.ModuleType, GetVertexGraphConfiguration(), vertexConfig, LogConfiguration);
            await _craClient.InstantiateVertexAsync(
                target.InstanceNames.ToArray(),
                target.VertexName,
                vertexDefinition,
                hostConfig.BinarySerialize()
            ).ConfigureAwait(false);

            //foreach (var vertexConfig in target.ToConfigurations())
            //{
            //    var hostParameter = new HostConfiguration(target.ModuleType, GetVertexGraphConfiguration(), vertexConfig, LogConfiguration);
                
                
            //    await _craClient.InstantiateVertexAsync(
            //        vertexConfig.InstanceName,
            //        target.VertexName,
            //        vertexDefinition,
            //        hostParameter.BinarySerialize(),
            //        i
            //    ).ConfigureAwait(false);
                
            //    i++;
            //}            
        }
    }
}
