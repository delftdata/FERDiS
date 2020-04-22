using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Extensions
{
    internal static class CRAClientLibraryExtensions
    {
        public static async Task ResetClusterAsync(this CRAClientLibrary client)
        {
            IVertexInfoProvider _vertexInfoProvider = client.DataProvider.GetVertexInfoProvider();
            IEndpointInfoProvider _endpointInfoProvider = client.DataProvider.GetEndpointInfoProvider();
            IVertexConnectionInfoProvider _vertexConnectionInfoProvider = client.DataProvider.GetVertexConnectionInfoProvider();
            IShardedVertexInfoProvider _shardedVertexInfoProvider = client.DataProvider.GetShardedVertexInfoProvider();

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
