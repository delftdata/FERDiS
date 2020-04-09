using BlackSP.Core.Endpoints;
using BlackSP.CRA.Configuration.Operators;
using BlackSP.CRA.Extensions;
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

    //kubernetes requirements..
    //make deployment.yaml file in folder?
    //print launch and inspect commands on console?
    public class CRAOperatorGraphConfiguratorBase
    {
        public ICollection<IOperatorConfigurator> Configurators { get; }

        private Dictionary<string, int> usedOperatorNameCount;
        private int usedInstanceCount;
        private readonly CRAClientLibrary craClient;

        protected CRAOperatorGraphConfiguratorBase(CRAClientLibrary craclient)
        {
            craClient = craclient ?? throw new ArgumentNullException(nameof(craclient));
            Configurators = new List<IOperatorConfigurator>();
            usedInstanceCount = 0;
            usedOperatorNameCount = new Dictionary<string, int>();
        }

        protected async Task RegisterGraphWithCRA()
        {
            await craClient.ResetClusterAsync();

            string craVertexName = typeof(OperatorVertex).Name.ToLowerInvariant();
            await craClient.DefineVertexAsync(craVertexName, () => new OperatorVertex());

            foreach (var configurator in Configurators)
            {
                await RegisterCRAVertexAsync(configurator, craVertexName);
            }

            foreach (var edge in Configurators.SelectMany(c => c.OutgoingEdges))
            {              
                await craClient.ConnectAsync(edge.FromOperator.OperatorName, edge.FromEndpoint, edge.ToOperator.OperatorName, edge.ToEndpoint);
            }
        }

        private async Task RegisterCRAVertexAsync(IOperatorConfigurator target, string vertexDefinition)
        {
            var vertexParameter = new VertexParameter(
                target.OperatorType,
                target.OperatorConfigurationType,
                target.InputEndpointNames.ToArray(),
                typeof(InputEndpoint),
                target.OutputEndpointNames.ToArray(),
                typeof(OutputEndpoint),
                typeof(ProtobufSerializer)
            );

            await craClient.InstantiateVertexAsync(
                target.InstanceNames,
                target.OperatorName,
                vertexDefinition,
                vertexParameter,
                1
            );
        }

        /// <summary>
        /// Returns a unique operator name for every invocation. To be used to name cra vertices
        /// </summary>
        /// <returns></returns>
        protected string GetNextAvailableOperatorName(string prefix)
        {
            if(!usedOperatorNameCount.ContainsKey(prefix))
            {
                usedOperatorNameCount.Add(prefix, 0);
            }
            return $"{prefix}{++usedOperatorNameCount[prefix]:D2}";
        }

        /// <summary>
        /// Returns a unique instance name for every invocation. To be used to name machine instances
        /// </summary>
        /// <returns></returns>
        protected string GetNextAvailableInstanceName()
        {
            return $"crainst{++usedInstanceCount:D2}";
        }

        /// <summary>
        /// Returns a set of unique instance names for every invocation. To be used to name machine instances
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<string> GetNextAvailableInstanceNames(int count)
        {
            for(int i = 0; i < count; i++)
            {
                yield return GetNextAvailableInstanceName();
            }
        }

    }
}
