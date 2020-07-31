using BlackSP.Core.Endpoints;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using CRA.ClientLibrary;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexOutputEndpoint : IAsyncShardedVertexOutputEndpoint
    {
        public delegate VertexOutputEndpoint Factory(IOutputEndpoint outputEndpoint);

        private readonly IOutputEndpoint _bspOutputEndpoint;
        private readonly ILogger _logger;

        public VertexOutputEndpoint(IOutputEndpoint outputEndpoint, ILogger logger)
        {
            _bspOutputEndpoint = outputEndpoint ?? throw new ArgumentNullException(nameof(outputEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ToStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            try
            {
                //CRA invokes current method on a background thread so just invoke Egress on this thread
                await _bspOutputEndpoint.Egress(stream, otherEndpoint, otherShardId, token).ConfigureAwait(false);
            }
            finally
            {
                _logger.Debug($"Stopped egressing data to vertex {otherVertex}${otherShardId} on endpoint {otherEndpoint}");

            }
            token.ThrowIfCancellationRequested();
        }

        public void UpdateShardingInfo(string otherVertex, ShardingInfo shardingInfo)
        {} //not supported in BlackSP

        public Task ToStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            //We dont need this method as we only use sharded CRA vertices
            throw new NotImplementedException($"Wrong ToStreamAsync in {this.GetType().Name}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            { }
        }
    }
}
