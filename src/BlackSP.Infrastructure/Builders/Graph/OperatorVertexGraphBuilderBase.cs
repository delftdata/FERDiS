using BlackSP.Checkpointing;
using BlackSP.Infrastructure.Builders.Edge;
using BlackSP.Infrastructure.Builders.Vertex;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Builders.Graph
{
    public abstract class OperatorVertexGraphBuilderBase : IVertexGraphBuilder
    {
        public ICollection<IVertexBuilder> VertexBuilders { get; }

        private Dictionary<string, int> usedOperatorNameCount;
        private int usedInstanceCount;
        
        protected ILogConfiguration LogConfiguration { get; private set; }
        protected ICheckpointConfiguration CheckpointConfiguration { get; private set; }

        protected OperatorVertexGraphBuilderBase()
        {
            VertexBuilders = new List<IVertexBuilder>();
            usedInstanceCount = 0;
            usedOperatorNameCount = new Dictionary<string, int>();
        }

        /// <summary>
        /// Implement infrastructure specific graph build
        /// </summary>
        /// <returns></returns>
        protected abstract Task<IApplication> BuildGraph();

        /// <summary>
        /// Builds the graph as configured by user with a coordinator connected to all existing vertices
        /// </summary>
        /// <returns></returns>
        public async Task<IApplication> Build(ILogConfiguration logConfiguration, ICheckpointConfiguration checkpointConfiguration)
        {
            LogConfiguration = logConfiguration ?? throw new ArgumentNullException(nameof(logConfiguration));
            CheckpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));

            AddCoordinator();
            return await BuildGraph().ConfigureAwait(false);
        }

        public IVertexGraphConfiguration GetVertexGraphConfiguration()
        {
            var allInstances = new List<string>();
            var allConnections = new List<Tuple<string, string>>();

            foreach (var vertexBuilder in VertexBuilders.Where(c => !c.VertexName.Contains("coordinator")))
            {
                //get instance names
                //only use outgoing connections to build tuples

                var instanceNames = vertexBuilder.InstanceNames;
                var targetVertices = vertexBuilder.OutgoingEdges.Select(e => e.ToVertex).Where(v => !v.VertexName.Contains("coordinator"));

                //var targetInstanceNames = targetVertices.SelectMany(v => v.InstanceNames);

                var fromShardId = 0;
                foreach(var instanceName in instanceNames)
                {
                    foreach(var edge in vertexBuilder.OutgoingEdges.Where(v => !v.ToVertex.VertexName.Contains("coordinator")))
                    {
                        if(edge.IsPipeline())
                        {
                            allConnections.Add(Tuple.Create(instanceName, edge.ToVertex.InstanceNames.ElementAt(fromShardId)));
                        } 
                        else //no pipeline, there is a shuffle/mesh connection between these vertices
                        {
                            foreach (var targetInst in edge.ToVertex.InstanceNames)
                            {
                                allConnections.Add(Tuple.Create(instanceName, targetInst));
                            }
                        }
                    }
                    fromShardId++;
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
            var coordinatorConfigurator = new CoordinatorVertexBuilder(new string[] { instanceName }, vertexName);
            //connect to all existing configurators (all workers)
            foreach (var configurator in VertexBuilders)
            {
                var fromCoordinatorEdge = new EdgeBuilder(coordinatorConfigurator, coordinatorConfigurator.GetAvailableOutputEndpoint(), configurator, configurator.GetAvailableInputEndpoint())
                    .AsShuffle();
                var toCoordinatorEdge = new EdgeBuilder(configurator, configurator.GetAvailableOutputEndpoint(), coordinatorConfigurator, coordinatorConfigurator.GetAvailableInputEndpoint())
                    .AsShuffle();

                coordinatorConfigurator.OutgoingEdges.Add(fromCoordinatorEdge);
                coordinatorConfigurator.IncomingEdges.Add(toCoordinatorEdge);

                configurator.IncomingEdges.Add(fromCoordinatorEdge);
                configurator.OutgoingEdges.Add(toCoordinatorEdge);

            }
            VertexBuilders.Add(coordinatorConfigurator);
        }

        public AggregateOperatorVertexBuilder<TOperator, TIn, TOut> AddAggregate<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IAggregateOperator<TIn, TOut>
            where TIn : class, IEvent
            where TOut : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("aggregate");
            var configurator = new AggregateOperatorVertexBuilder<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
            return configurator;
        }

        public FilterOperatorVertexBuilder<TOperator, TEvent> AddFilter<TOperator, TEvent>(int shardCount)
            where TOperator : IFilterOperator<TEvent>
            where TEvent : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("filter");
            var configurator = new FilterOperatorVertexBuilder<TOperator, TEvent>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
            return configurator;
        }

        public JoinOperatorVertexBuilder<TOperator, TIn1, TIn2, TOut> AddJoin<TOperator, TIn1, TIn2, TOut>(int shardCount)
            where TOperator : IJoinOperator<TIn1, TIn2, TOut>
            where TIn1 : class, IEvent
            where TIn2 : class, IEvent
            where TOut : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("join");
            var configurator = new JoinOperatorVertexBuilder<TOperator, TIn1, TIn2, TOut>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
            return configurator;
        }

        public MapOperatorVertexBuilder<TOperator, TIn, TOut> AddMap<TOperator, TIn, TOut>(int shardCount)
            where TOperator : IMapOperator<TIn, TOut>
            where TIn : class, IEvent
            where TOut : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("map");
            var configurator = new MapOperatorVertexBuilder<TOperator, TIn, TOut>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
            return configurator;
        }

        public SinkOperatorVertexBuilder<TOperator, TIn> AddSink<TOperator, TIn>(int shardCount)
            where TOperator : ISinkOperator<TIn>
            where TIn : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("sink");
            var configurator = new SinkOperatorVertexBuilder<TOperator, TIn>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
            return configurator;
        }

        public SourceOperatorVertexBuilder<TOperator, TOut> AddSource<TOperator, TOut>(int shardCount)
            where TOperator : ISourceOperator<TOut>
            where TOut : class, IEvent
        {
            var instanceNames = GetNextAvailableInstanceNames(shardCount);
            var vertexName = GetNextAvailableOperatorName("source");
            var configurator = new SourceOperatorVertexBuilder<TOperator, TOut>(instanceNames.ToArray(), vertexName);
            VertexBuilders.Add(configurator);
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
