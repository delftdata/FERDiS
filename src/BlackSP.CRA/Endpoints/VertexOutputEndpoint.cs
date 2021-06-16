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
    public class VertexOutputEndpoint : IAsyncShardedVertexOutputEndpoint
    {
        public delegate VertexOutputEndpoint Factory(IEndpointConfiguration config);

        private readonly IOutputEndpoint _bspOutputEndpoint;
        private readonly IVertexGraphConfiguration _graphConfig;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _epConfig;
        private readonly ILogger _logger;

        public VertexOutputEndpoint(IEndpointConfiguration config,
            IVertexGraphConfiguration graphConfig,
            IVertexConfiguration vertexConfig, 
            EndpointFactory endpointFactory, 
            ILogger logger)
        {
            _epConfig = config ?? throw new ArgumentNullException(nameof(config));
            _graphConfig = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));
            _vertexConfig = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));
            _ = endpointFactory ?? throw new ArgumentNullException(nameof(endpointFactory));
            _bspOutputEndpoint = endpointFactory.ConstructOutputEndpoint(config, true);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ToStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            try
            {
                if (!_epConfig.IsControl && !_epConfig.IsBackchannel && !_graphConfig.GetAllInstancesDownstreamOf(_vertexConfig.InstanceName, true).Contains(_epConfig.GetRemoteInstanceName(otherShardId)))
                {
                    _logger.Information("Holding off output connection to " + _epConfig.GetRemoteInstanceName(otherShardId) + ", suspecting its not part of the pipeline");
                    await Task.Delay(-1, token);
                }
                //CRA invokes current method on a background thread so just invoke Egress on this thread
                await _bspOutputEndpoint.Egress(stream, otherEndpoint, otherShardId, token).ConfigureAwait(false);
            }
            finally
            {
                _logger.Debug($"Stopped egressing data to vertex {otherVertex}${otherShardId} on endpoint {otherEndpoint}");
                await stream.DisposeAsync();
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
