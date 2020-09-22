using BlackSP.Core.Endpoints;
using BlackSP.Infrastructure.Layers.Common;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using CRA.ClientLibrary;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            _bspInputEndpoint = endpointFactory.ConstructInputEndpoint(config);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            //wraps overload as for input channels, we dont care which shard of other vertex it came from
            try
            {
                //CRA invokes this method on a background thread so just invoke Ingress on current thread
                await _bspInputEndpoint.Ingress(stream, otherEndpoint, otherShardId, token).ConfigureAwait(false);
            }
            finally
            {
                _logger.Debug($"Stopped ingressing data from vertex {otherVertex}${otherShardId} from endpoint {otherEndpoint}");
            }
            token.ThrowIfCancellationRequested();
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
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
