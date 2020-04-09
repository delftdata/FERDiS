using BlackSP.CRA.Configuration.Operators;
using BlackSP.CRA.Kubernetes;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    internal class OperatorGraphConfigurator : CRAOperatorGraphConfiguratorBase, IOperatorGraphConfigurator
    {

        private readonly KubernetesDeploymentUtility k8sDeploymentUtil;

        public OperatorGraphConfigurator(KubernetesDeploymentUtility k8sUtil, CRAClientLibrary craClient) : base(craClient)
        {
            k8sDeploymentUtil = k8sUtil ?? throw new ArgumentNullException(nameof(k8sUtil));
        }

        /// <summary>
        /// Registers the constructed operator graph with cra and prepares deployment files for kubernetes
        /// </summary>
        /// <returns></returns>
        public async Task BuildGraph()
        {
            await RegisterGraphWithCRA();
            k8sDeploymentUtil.With(Configurators).WriteDeploymentYaml();
            k8sDeploymentUtil.PrintUsage();
        }

        //Note the explicit interface implementations below, this is to avoid duplicating generic type constraints from the interface
        //this makes the methods only available when casting object instance to the interface, and they cannot be marked public

        IAggregateOperatorConfigurator<TOperator, TIn, TOut> IOperatorGraphConfigurator.AddAggregate<TOperator, TIn, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("aggregate");
            var configurator = new AggregateOperatorConfigurator<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IFilterOperatorConfigurator<TOperator, TEvent> IOperatorGraphConfigurator.AddFilter<TOperator, TEvent>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("filter");
            var configurator = new FilterOperatorConfigurator<TOperator, TEvent>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> IOperatorGraphConfigurator.AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("join");
            var configurator = new JoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IMapOperatorConfigurator<TOperator, TIn, TOut> IOperatorGraphConfigurator.AddMap<TOperator, TIn, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("map");
            var configurator = new MapOperatorConfigurator<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        ISinkOperatorConfigurator<TOperator, TIn> IOperatorGraphConfigurator.AddSink<TOperator, TIn>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("sink");
            var configurator = new SinkOperatorConfigurator<TOperator, TIn>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        ISourceOperatorConfigurator<TOperator, TOut> IOperatorGraphConfigurator.AddSource<TOperator, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("source");
            var configurator = new SourceOperatorConfigurator<TOperator, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }
    }
}
