using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
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

    public class OutputEndpoint<TMessage> : IOutputEndpoint
        where TMessage : IMessage
    {

        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate OutputEndpoint<TMessage> Factory(string endpointName);

        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public OutputEndpoint(string endpointName, 
            IDispatcher<TMessage> dispatcher, 
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
            using CancellationTokenSource exceptionSource = new CancellationTokenSource();
            using CancellationTokenSource callerOrExceptionSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, exceptionSource.Token);
            string targetInstanceName = _endpointConfig.GetRemoteInstanceName(remoteShardId);
            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting output stream writer. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");      
            try
            {
                var pipe = outputStream.UsePipe(cancellationToken: callerOrExceptionSource.Token);
                using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);
                using PipeStreamReader reader = new PipeStreamReader(pipe.Input);
                using SemaphoreSlim queueAccess = new SemaphoreSlim(1, 1);
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var writingThread = StartWritingOutput(writer, remoteShardId, queueAccess, callerOrExceptionSource.Token);
                var readingThread = StartFlushRequestListener(reader, remoteShardId, queueAccess, callerOrExceptionSource.Token);
                var exitedThread = await Task.WhenAny(writingThread, readingThread).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} to {_endpointConfig.GetRemoteInstanceName(remoteShardId)} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Output endpoint {_endpointConfig.LocalEndpointName} output stream writer ran into an exception. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");
                exceptionSource.Cancel();
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
            }
        }

        /// <summary>
        /// Starts writing output from provided msgQueue
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="msgQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task StartWritingOutput(PipeStreamWriter writer, int shardId, SemaphoreSlim queueAccess, CancellationToken t)
        {
            var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
            while (!t.IsCancellationRequested)
            {
                await queueAccess.WaitAsync(t).ConfigureAwait(false);

                using var timeoutSource = new CancellationTokenSource(1000);
                using var lcts = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);
                try
                {
                    var message = await dispatchQueue.UnderlyingCollection.Reader.ReadAsync(lcts.Token);
                    await writer.WriteMessage(message, t).ConfigureAwait(false);
                    if (message.IsFlushMessage())
                    {
                        await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    //there was no message to dispatch before timeout
                    //flush whatever is still in the output buffer
                    await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                }
                queueAccess.Release();
            }
            t.ThrowIfCancellationRequested();
        }

        private async Task StartFlushRequestListener(PipeStreamReader reader, int shardId, SemaphoreSlim queueAccess, CancellationToken t)
        {
            var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
            while (!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();
                _logger.Verbose($"Output endpoint {_endpointConfig.LocalEndpointName} flush listener waiting for next message from {_endpointConfig.GetRemoteInstanceName(shardId)}");
                var message = await reader.ReadNextMessage(t).ConfigureAwait(false);
                if (message?.IsFlushMessage() ?? false)
                {
                    await queueAccess.WaitAsync(t).ConfigureAwait(false);
                    _logger.Verbose($"Output endpoint {_endpointConfig.LocalEndpointName} handling flush message from {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    await dispatchQueue.EndFlush().ConfigureAwait(false);
                    if(!await dispatchQueue.UnderlyingCollection.Writer.WaitToWriteAsync(t))
                    {
                        throw new InvalidOperationException("Dispatch queue writer completed, invalid program state");
                    }
                    await dispatchQueue.UnderlyingCollection.Writer.WriteAsync(message, t); //after flush the dispatchqueue is empty, add the flush message to signal completion downstream
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} ended dispatcher queue flush, flush message sent back to downstream instance: {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    queueAccess.Release();
                }
                else
                {
                    _logger.Fatal($"Output endpoint {_endpointConfig.LocalEndpointName} received invalid instruction on flush listener from instance {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    throw new InvalidOperationException($"FlushRequestListener expected flush message but received: {message}");
                }
            }
            t.ThrowIfCancellationRequested();
        }
    }
}
