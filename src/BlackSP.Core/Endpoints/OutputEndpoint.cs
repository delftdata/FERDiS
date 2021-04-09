using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Observers;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Streams;
using Nerdbank.Streams;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
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
        private readonly ConnectionObserver _connectionMonitor;
        private readonly ILogger _logger;

        public OutputEndpoint(string endpointName, 
            IDispatcher<TMessage> dispatcher, 
            IVertexConfiguration vertexConfiguration, 
            ConnectionObserver connectionMonitor,
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
            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} starting egress.");      
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
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} egress exited with an exception.");
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
                try
                {                
                    using var lcts = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);

                    dispatchQueue.ThrowIfFlushingStarted();
                    byte[] message;
                    try
                    {
                        message = await dispatchQueue.UnderlyingCollection.Reader.ReadAsync(lcts.Token);
                    } 
                    catch(ChannelClosedException)
                    {
                        dispatchQueue.ThrowIfFlushingStarted();
                        throw;
                    }
                    await writer.WriteMessage(message, t).ConfigureAwait(false);
                    if (message.IsFlushMessage() || _endpointConfig.IsControl)
                    {
                        await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                    }
                    queueAccess.Release();

                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    //there was no message to dispatch before timeout
                    //flush whatever is still in the output buffer
                    await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                    queueAccess.Release();
                }
                catch (OperationCanceledException) when (t.IsCancellationRequested)
                {
                    //caller cancelled, leave method in consistent state .. release semaphore
                    queueAccess.Release();
                    throw;
                }
                catch (FlushInProgressException)
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {_endpointConfig.GetRemoteInstanceName(shardId)} paused stream writer to wait for flush");
                    var ongoingFlush = dispatchQueue.BeginFlush();
                    queueAccess.Release();
                    await ongoingFlush.ConfigureAwait(false); //Join the wait for flush completion.. the flushrequest listener will complete it..
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {_endpointConfig.GetRemoteInstanceName(shardId)} unpaused stream writer after flush");
                }
            }
            t.ThrowIfCancellationRequested();
        }

        private async Task StartFlushRequestListener(PipeStreamReader reader, int shardId, SemaphoreSlim queueAccess, CancellationToken t)
        {
            var targetInstanceName = _endpointConfig.GetRemoteInstanceName(shardId);

            var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
            while (!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();
                _logger.Verbose($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {targetInstanceName} flush listener waiting for next message.");
                var message = await reader.ReadNextMessage(t).ConfigureAwait(false);
                if (message?.IsFlushMessage() ?? false)
                {                    
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {targetInstanceName} received flush message.");
                    await queueAccess.WaitAsync(t).ConfigureAwait(false);
                    try
                    {
                        await dispatchQueue.EndFlush().ConfigureAwait(false);
                        _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {targetInstanceName} ended dispatcher queue flush");
                        if (!await dispatchQueue.UnderlyingCollection.Writer.WaitToWriteAsync(t))
                        {
                            throw new InvalidOperationException("Dispatch queue writer completed, invalid program state");
                        }
                        if (!dispatchQueue.UnderlyingCollection.Writer.TryWrite(message)) //after flush the dispatchqueue is empty, add the flush message to signal completion downstream
                        {
                            throw new InvalidOperationException("Got channel write access but could not write");
                        }
                        _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {targetInstanceName} sent flush message response downstream.");

                    }
                    finally
                    {
                        queueAccess.Release();
                    }
                }
                else
                {
                    _logger.Error($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {targetInstanceName} received unknown message type on flush listener.");
                    throw new InvalidOperationException($"FlushRequestListener expected flush message but received: {message}");
                }
            }
            t.ThrowIfCancellationRequested();
        }
    }
}
