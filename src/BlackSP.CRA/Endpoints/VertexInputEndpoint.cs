using BlackSP.Infrastructure.Factories;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using CRA.ClientLibrary;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexInputEndpoint : IAsyncShardedVertexInputEndpoint
    {
        public delegate VertexInputEndpoint Factory(IEndpointConfiguration config);

        private readonly IInputEndpoint _bspInputEndpoint;
        private readonly IVertexGraphConfiguration _graphConfig;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _epConfig;
        private readonly ILogger _logger;

        public VertexInputEndpoint(IEndpointConfiguration config, 
            IVertexGraphConfiguration graphConfig,
            IVertexConfiguration vertexConfig,
            EndpointFactory endpointFactory,
            ILogger logger)
        {
            _epConfig = config ?? throw new ArgumentNullException(nameof(config));
            _graphConfig = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));
            _vertexConfig = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));
            _ = endpointFactory ?? throw new ArgumentNullException(nameof(endpointFactory));
            _bspInputEndpoint = endpointFactory.ConstructInputEndpoint(config, true);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            try
            {
                if(!_epConfig.IsControl && !_epConfig.IsBackchannel && !_graphConfig.GetAllInstancesUpstreamOf(_vertexConfig.InstanceName, true).Contains(_epConfig.GetRemoteInstanceName(otherShardId))) {
                    _logger.Information("Holding off input connection to " + _epConfig.GetRemoteInstanceName(otherShardId) + ", suspecting its not part of the pipeline");
                    await Task.Delay(-1, token);
                }
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
