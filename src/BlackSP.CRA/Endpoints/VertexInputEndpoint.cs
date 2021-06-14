using BlackSP.Infrastructure.Factories;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using CRA.ClientLibrary;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexInputEndpoint : IAsyncShardedVertexInputEndpoint
    {
        public delegate VertexInputEndpoint Factory(IEndpointConfiguration config);

        private readonly IInputEndpoint _bspInputEndpoint;
        private readonly ILogger _logger;

        public VertexInputEndpoint(IEndpointConfiguration config, EndpointFactory endpointFactory, ILogger logger)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = endpointFactory ?? throw new ArgumentNullException(nameof(endpointFactory));
            _bspInputEndpoint = endpointFactory.ConstructInputEndpoint(config, true);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            try
            {
                //CRA invokes this method on the thread pool so just invoke Ingress here..
                await _bspInputEndpoint.Ingress(stream, otherEndpoint, otherShardId, token).ConfigureAwait(false);
            }
            finally
            {
                _logger.Debug($"Stopped ingressing data from vertex {otherVertex}${otherShardId} from endpoint {otherEndpoint}");
                await stream.DisposeAsync();
            }
            token.ThrowIfCancellationRequested();
        }

        public Task FromStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            throw new NotSupportedException("Wrong FromStreamAsync invoked");
        }

        public void UpdateShardingInfo(string otherVertex, ShardingInfo shardingInfo)
        {
            //No need to care about sharding info on an input endpoint
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
