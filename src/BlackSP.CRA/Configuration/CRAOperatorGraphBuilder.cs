using BlackSP.CRA.Extensions;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.Serialization.Extensions;
using CRA.ClientLibrary;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    class CRAOperatorGraphBuilder : OperatorVertexGraphBuilderBase
    {
        private readonly KubernetesDeploymentUtility _k8sDeploymentUtil;
        private readonly CRAClientLibrary _craClient;
        private readonly TextWriter _defaultConsoleOut;

        public CRAOperatorGraphBuilder(KubernetesDeploymentUtility k8sUtil, CRAClientLibrary craClient) : base()
        {
            _k8sDeploymentUtil = k8sUtil ?? throw new ArgumentNullException(nameof(k8sUtil));
            _craClient = craClient ?? throw new ArgumentNullException(nameof(craClient));
            _defaultConsoleOut = Console.Out;
        }

        /// <summary>
        /// Little utility method to get rid of the huge mess CRA prints on the console
        /// </summary>
        /// <param name="silenced"></param>
        private void SetConsoleSilenced(bool silenced)
        {
            Console.SetOut(silenced ? TextWriter.Null : _defaultConsoleOut);
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
            Console.WriteLine("Resetting CRA vertex cluster");
            await _craClient.ResetClusterAsync().ConfigureAwait(false);

            string craVertexDefinition = typeof(OperatorVertex).Name.ToLowerInvariant();
            Console.WriteLine($"Defining CRA vertex type {typeof(OperatorVertex)} as {craVertexDefinition}");
            await _craClient.DefineVertexAsync(craVertexDefinition, () => new OperatorVertex()).ConfigureAwait(false);

            foreach (var builder in VertexBuilders)
            {
                await RegisterCRAVertexAsync(builder, craVertexDefinition).ConfigureAwait(false);
            }

            foreach (var edge in VertexBuilders.SelectMany(c => c.OutgoingEdges))
            {
                await RegisterCRAVertexConnectionAsync(edge).ConfigureAwait(false);
            }

            Console.WriteLine("CRA vertex cluster registration completed");
        } 

        private async Task RegisterCRAVertexAsync(IVertexBuilder target, string vertexDefinition)
        {
            Console.WriteLine($"Registering Vertex {target.VertexName} on instances {string.Join(", ", target.InstanceNames)}");

            SetConsoleSilenced(true);
            
            var vertexConfig = target.GetVertexConfiguration();
            var hostConfig = new HostConfiguration(target.ModuleType, GetVertexGraphConfiguration(), vertexConfig, LogConfiguration);
            await _craClient.InstantiateVertexAsync(
                target.InstanceNames.ToArray(),
                target.VertexName,
                vertexDefinition,
                hostConfig.BinarySerialize()
            ).ConfigureAwait(false);
            
            SetConsoleSilenced(false);
        }

        private async Task RegisterCRAVertexConnectionAsync(Edge edge)
        {
            Console.WriteLine($"Registering connection \"{edge.FromVertex.VertexName} {edge.FromEndpoint}\" to \"{edge.ToVertex.VertexName} {edge.ToEndpoint}\"");
            SetConsoleSilenced(true);
            await _craClient.ConnectAsync(edge.FromVertex.VertexName, edge.FromEndpoint, edge.ToVertex.VertexName, edge.ToEndpoint).ConfigureAwait(false);
            SetConsoleSilenced(false);
        }
    }
}
