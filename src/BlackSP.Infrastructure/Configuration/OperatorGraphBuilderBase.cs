using BlackSP.Infrastructure.Configuration.Vertices;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Configuration
{
    public abstract class OperatorGraphBuilderBase : OperatorGraphBuilderBase<object>
    { }

    public abstract class OperatorGraphBuilderBase<TGraph> : IOperatorGraphBuilder
    {
        public ICollection<IVertexConfigurator> Configurators { get; }

        private Dictionary<string, int> usedOperatorNameCount;
        private int usedInstanceCount;

        protected OperatorGraphBuilderBase()
        {
            Configurators = new List<IVertexConfigurator>();
            usedInstanceCount = 0;
            usedOperatorNameCount = new Dictionary<string, int>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract Task<TGraph> BuildGraph();

        /// <summary>
        /// Builds the graph as configured by user with a coordinator connected to all 
        /// </summary>
        /// <returns></returns>
        public async Task<TGraph> Build()
        {
            AddCoordinator();
            return await BuildGraph().ConfigureAwait(false);
        }

        public IVertexGraphConfiguration GetVertexGraphConfiguration()
        {
            var allInstances = new List<string>();
            var allConnections = new List<Tuple<string, string>>();

            foreach (var configurator in Configurators.Where(c => !c.VertexName.Contains("coordinator")))
            {
                //get instance names
                //only use outgoing connections to build tuples

                var instanceNames = configurator.InstanceNames;
                var targetInstanceNames = configurator.OutgoingEdges.Select(e => e.ToVertex).Where(v => !v.VertexName.Contains("coordinator")).SelectMany(v => v.InstanceNames);

                foreach(var instanceName in instanceNames)
                {
                    foreach(var targetInst in targetInstanceNames)
                    {
                        allConnections.Add(Tuple.Create(instanceName, targetInst));
                    }
                }
                allInstances.AddRange(instanceNames);
            }

            return new VertexGraphConfiguration(allInstances, allConnections);
        }

        /// <summary>
        /// Adds a coordinator vertex to the graph configuration, will create edges from and to every so far created vertex.
        /// </summary>
        private void AddCoordinator()
        {
            var instanceName = GetNextAvailableInstanceName(); //coordinator is never sharded
            var vertexName = GetNextAvailableOperatorName("coordinator");
            var coordinatorConfigurator = new CoordinatorConfigurator(new string[] { instanceName }, vertexName);
            //connect to all existing configurators (all workers)
            foreach (var configurator in Configurators)
            {
                var fromCoordinatorEdge = new Edge(coordinatorConfigurator, coordinatorConfigurator.GetAvailableOutputEndpoint(), configurator, configurator.GetAvailableInputEndpoint());
                var toCoordinatorEdge = new Edge(configurator, configurator.GetAvailableOutputEndpoint(), coordinatorConfigurator, coordinatorConfigurator.GetAvailableInputEndpoint());

                coordinatorConfigurator.OutgoingEdges.Add(fromCoordinatorEdge);
                coordinatorConfigurator.IncomingEdges.Add(toCoordinatorEdge);

                configurator.IncomingEdges.Add(fromCoordinatorEdge);
                configurator.OutgoingEdges.Add(toCoordinatorEdge);

            }
            Configurators.Add(coordinatorConfigurator);
        }

        //Note the explicit interface implementations below, this is to avoid duplicating generic type constraints from the interface
        //this makes the methods only available when casting object instance to the interface, and they cannot be marked public (even though they are)

        IAggregateOperatorConfigurator<TOperator, TIn, TOut> IOperatorGraphBuilder.AddAggregate<TOperator, TIn, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("aggregate");
            var configurator = new AggregateOperatorConfigurator<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IFilterOperatorConfigurator<TOperator, TEvent> IOperatorGraphBuilder.AddFilter<TOperator, TEvent>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("filter");
            var configurator = new FilterOperatorConfigurator<TOperator, TEvent>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> IOperatorGraphBuilder.AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("join");
            var configurator = new JoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        IMapOperatorConfigurator<TOperator, TIn, TOut> IOperatorGraphBuilder.AddMap<TOperator, TIn, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("map");
            var configurator = new MapOperatorConfigurator<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        ISinkOperatorConfigurator<TOperator, TIn> IOperatorGraphBuilder.AddSink<TOperator, TIn>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("sink");
            var configurator = new SinkOperatorConfigurator<TOperator, TIn>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
        }

        ISourceOperatorConfigurator<TOperator, TOut> IOperatorGraphBuilder.AddSource<TOperator, TOut>(int shardCount)
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("source");
            var configurator = new SourceOperatorConfigurator<TOperator, TOut>(instanceNames.ToArray(), vertexName);
            Configurators.Add(configurator);
            return configurator;
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
