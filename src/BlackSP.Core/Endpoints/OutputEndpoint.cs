using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Streams;
using Nerdbank.Streams;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
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
        public async Task Egress(Stream outputStream, string remoteEndpointName, int remoteShardId, CancellationToken callerToken)
        {
            _ = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

            string targetInstanceName = _endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId);
            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting output stream writer. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");

            var pipe = outputStream.UsePipe(cancellationToken: callerToken);
            using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);            
            try
            {
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var msgQueue = _dispatcher.GetDispatchQueue(_endpointConfig, remoteShardId);
                await StartWritingOutput(writer, msgQueue, callerToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Output endpoint {_endpointConfig.LocalEndpointName} output stream writer ran into an exception. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
            }
        }

        /// <summary>
        /// Starts writing output from provided msgQueue, inserts ping messages on the channel for keepalive-check purposes.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="msgQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task StartWritingOutput(PipeStreamWriter writer, BlockingCollection<byte[]> msgQueue, CancellationToken t)
        {
            while(!t.IsCancellationRequested)
            {
                Task action = msgQueue.TryTake(out var msg, 1000, t) 
                    ? writer.WriteMessage(msg, t) 
                    : writer.FlushAndRefreshBuffer(t: t);
                await action.ConfigureAwait(false);
            }
            t.ThrowIfCancellationRequested();
        }
    }
}
