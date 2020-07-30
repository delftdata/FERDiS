using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Streams;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint
    {
        public delegate OutputEndpoint Factory(string endpointName);

        private readonly IDispatcher<IMessage> _dispatcher;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public OutputEndpoint(string endpointName, 
            IDispatcher<IMessage> dispatcher, 
            IVertexConfiguration vertexConfiguration, 
            ConnectionMonitor connectionMonitor,
            ILogger logger)
        {
            _ = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            _vertexConfig = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            
            _endpointConfig = _vertexConfig.OutputEndpoints.First(x => x.LocalEndpointName == endpointName);

            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        /// <summary>
        /// Starts a blocking loop that will check the registered remote shard's output queue for
        /// new events and write them to the provided outputstream.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        public async Task Egress(Stream outputStream, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            _ = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

            var msgBytesBuffer = _dispatcher.GetDispatchQueue(_endpointConfig, remoteShardId);
            var writer = new PipeStreamWriter(outputStream, _endpointConfig.IsControl);
            
            try
            {
                t.ThrowIfCancellationRequested();
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting serialize & write threads. Writing to \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                while(!t.IsCancellationRequested)
                {
                    if(!msgBytesBuffer.TryTake(out var message, 5 * 1000, t))
                    {
                        _logger.Verbose($"Inactivity on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName} on {_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}");
                        continue;
                    }

                    //endpoint drops messages if dispatcher flags indicate there should not be dispatched
                    var endpointTypeDeliveryFlag = _endpointConfig.IsControl ? DispatchFlags.Control : DispatchFlags.Data;
                    if (_dispatcher.GetFlags().HasFlag(endpointTypeDeliveryFlag))
                    {
                        await writer.WriteMessage(message, t).ConfigureAwait(false);
                    }
                    _logger.Verbose($"Message written to {_endpointConfig.RemoteVertexName} on {_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}");

                    //await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                }
            } 
            catch(Exception e)
            {
                _logger.Warning(e, $"Output endpoint {_endpointConfig.LocalEndpointName} serialize & write threads ran into an exception. Writing to \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                throw;
            }
            finally
            {                
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
                writer.Dispose();
            }
            
        }
    }
}
