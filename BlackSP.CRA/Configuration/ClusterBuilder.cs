using BlackSP.Core.Operators;
using BlackSP.CRA.Events;
using BlackSP.CRA.Kubernetes;
using CRA.ClientLibrary;
using CRA.DataProvider;
using CRA.DataProvider.File;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    public class ClusterBuilder
    {
        //ClusterBuilder API
        // - track list of nodes to create
        // + NewSource : BSPSource (static?)
        // + Generate K8s yaml

        private CRAClientLibrary _craClient;
        private IVertexInfoProvider _vertexInfoProvider => _craClient.DataProvider.GetVertexInfoProvider();
        private IEndpointInfoProvider _endpointInfoProvider => _craClient.DataProvider.GetEndpointInfoProvider();
        private IVertexConnectionInfoProvider _vertexConnectionInfoProvider => _craClient.DataProvider.GetVertexConnectionInfoProvider();
        private IShardedVertexInfoProvider _shardedVertexInfoProvider => _craClient.DataProvider.GetShardedVertexInfoProvider();

        public ClusterBuilder(IDataProvider provider)
        {
            _craClient = new CRAClientLibrary(provider);
        }

        public CRAClientLibrary GetClientLibrary()
        {
            return _craClient;
        }

        public async Task ResetClusterAsync()
        {
            var vertexInfoDeleteTasks = (await _vertexInfoProvider.GetAll()).Select(v => _vertexInfoProvider.DeleteVertexInfo(v));
            var endpointInfoDeleteTasks = (await _endpointInfoProvider.GetAll()).Select(e => _endpointInfoProvider.DeleteEndpoint(e));
            var vertexConnectionInfoDeleteTasks = (await _vertexConnectionInfoProvider.GetAll()).Select(c => _vertexConnectionInfoProvider.Delete(c));
            var shardedVertexInfoDeleteTasks = (await _shardedVertexInfoProvider.GetAll()).Select(sv => _shardedVertexInfoProvider.Delete(sv));

            await Task.WhenAll(vertexInfoDeleteTasks
                .Concat(endpointInfoDeleteTasks)
                .Concat(vertexConnectionInfoDeleteTasks)
                .Concat(shardedVertexInfoDeleteTasks)
            );
        }

        public async Task TestMethod()
        {
            IOperatorGraphConfigurator graphConfigurator = new OperatorGraphConfigurator(new KubernetesDeploymentUtility(), new CRAClientLibrary());
            var filter1 = graphConfigurator.AddFilter<SampleFilterOperatorConfiguration, SampleEvent>(2);

            var mapper1 = graphConfigurator.AddMap<SampleMapOperatorConfiguration, SampleEvent, SampleEvent2>(2);

            filter1.Append(mapper1);
            //mapper1.Append(filter1);

            await graphConfigurator.BuildGraph();
            

            //var source = new SourceOperatorConfigurator<SampleEvent>(_craClient, "crainst01", "source01");
            //var filter = new FilterOperatorConfigurator<SampleFilterOperatorConfiguration, SampleEvent>(_craClient, "crainst02", "filter01");
            //await source.AppendAsync(filter);

            //var mapper = new MapOperatorConfigurator<SampleMapOperatorConfiguration, SampleEvent, SampleEvent2>(_craClient, "crainst03", "map01");
            //await filter.AppendAsync(mapper);

            //var sink = new SinkOperatorConfigurator<SampleEvent2>("crainst04", "sink01");
            //await mapper.AppendAsync(sink);
            //await mapper.AppendAsync(filter);


        }
    }

    class SampleMapOperatorConfiguration : IMapOperatorConfiguration<SampleEvent, SampleEvent2>
    {
        public IEnumerable<SampleEvent2> Map(SampleEvent @event)
        {
            throw new System.NotImplementedException();
        }
    }

    class SampleFilterOperatorConfiguration : IFilterOperatorConfiguration<SampleEvent>
    {
        public SampleEvent Filter(SampleEvent @event)
        {
            throw new System.NotImplementedException();
        }
    }

}
