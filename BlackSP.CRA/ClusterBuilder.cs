using CRA.ClientLibrary;
using CRA.DataProvider;
using CRA.DataProvider.File;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.CRA
{
    public class ClusterBuilder
    {
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
    }
}
