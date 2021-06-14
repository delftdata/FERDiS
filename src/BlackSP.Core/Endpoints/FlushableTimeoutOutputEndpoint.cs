using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Observers;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
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

    public class FlushableTimeoutOutputEndpoint<TMessage> : IOutputEndpoint
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate FlushableTimeoutOutputEndpoint<TMessage> Factory(string endpointName);

        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionObserver _connectionMonitor;
        private readonly ILogger _logger;

        public FlushableTimeoutOutputEndpoint(string endpointName,
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
            string targetInstanceName = _endpointConfig.GetRemoteInstanceName(remoteShardId);

            using CancellationTokenSource exceptionSource = new CancellationTokenSource();
            using CancellationTokenSource callerOrExceptionSource = CancellationTokenSource.CreateLinkedTokenSource(exceptionSource.Token, callerToken);
            var pipe = outputStream.UsePipe(cancellationToken: callerOrExceptionSource.Token);
            
            using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);
            using PipeStreamReader reader = new PipeStreamReader(pipe.Input);
            using SemaphoreSlim queueAccess = new SemaphoreSlim(1, 1);

            CancellationToken pongTimeoutToken = StartControlMessageListener(reader, remoteShardId, queueAccess, callerOrExceptionSource.Token);
            try
            {
                using CancellationTokenSource callerExceptionOrTimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(callerOrExceptionSource.Token, pongTimeoutToken);
                _logger.Information($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} starting egress.");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                await writer.WriteMessage(ControlMessageExtensions.ConstructKeepAliveMessage(), callerToken);//test: start by writing one message to ensure live network
                _logger.Information($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} wrote init message.");

                await WriteDispatchableMessages(writer, remoteShardId, queueAccess, callerExceptionOrTimeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Warning($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} is handling cancellation request from caller side");
                throw;
            }
            catch (OperationCanceledException) when (pongTimeoutToken.IsCancellationRequested)
            {
                _logger.Warning($"Output endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} to {targetInstanceName} keepalive-reception timeout, throwing IOException");
                exceptionSource.Cancel();
                throw new IOException($"Connection to {targetInstanceName} timed out, did not receive any keepalive messages for {Constants.KeepAliveTimeoutSeconds} seconds");
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
        /// Starts writing output from provided msgQueue, inserts ping messages on the channel for keepalive-check purposes.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="msgQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task WriteDispatchableMessages(PipeStreamWriter writer, int shardId, SemaphoreSlim queueAccess, CancellationToken t)
        {
            var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
            //bool hasAccess = false;
            while (!t.IsCancellationRequested)
            {
                await queueAccess.WaitAsync(t).ConfigureAwait(false);
                //hasAccess = true;
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
                    catch (ChannelClosedException)
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
                    //hasAccess = false;
                    await ongoingFlush.ConfigureAwait(false); //Join the wait for flush completion.. the flushrequest listener will complete it..
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName}${shardId} to {_endpointConfig.GetRemoteInstanceName(shardId)} unpaused stream writer after flush");
                }
                
            }
            t.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Listens for pong messages that are sent back in response to ping messages.<br/>
        /// Returns a cancellationtoken that gets cancelled if no pong is received within the specified timeout period
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private CancellationToken StartControlMessageListener(PipeStreamReader reader, int shardId, SemaphoreSlim queueAccess, CancellationToken t)
        {
            var targetInstanceName = _endpointConfig.GetRemoteInstanceName(shardId);
#pragma warning disable CA2000 // Dispose objects before losing scope
            //object gets disposed in task continuation
            var pongListenerTimeoutSource = new CancellationTokenSource(); //manually cancelled after a timeout occurs
#pragma warning restore CA2000 // Dispose objects before losing scope

            _ = Task.Run(async () =>
            {
                //await Task.Delay(Constants.KeepAliveTimeoutSeconds); //attempt to fix timeout shortly after connection establishment

                var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
                var lastPongReception = DateTimeOffset.Now;

                CancellationTokenSource pongTimeoutSource = null; //to track individual keepalive timeouts
                CancellationTokenSource linkedSource = null; //to track timeouts AND caller cancellation
                while (true)
                {
                    try
                    {                    
                        t.ThrowIfCancellationRequested();

                        TimeSpan timeTillNextTimeout = lastPongReception.AddSeconds(Constants.KeepAliveTimeoutSeconds) - DateTimeOffset.Now;
                        int msTillNextTimeout = (int)timeTillNextTimeout.TotalMilliseconds;

                        pongTimeoutSource = new CancellationTokenSource(Math.Max(msTillNextTimeout, 1)); //never wait negative
                        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutSource.Token);
                        _logger.Verbose($"awaiting control message on output endpoint: {_endpointConfig.LocalEndpointName} from {targetInstanceName} within {msTillNextTimeout}ms");

                        var message = await reader.ReadNextMessage(linkedSource.Token).ConfigureAwait(false);
                        if (message.IsKeepAliveMessage())
                        {
                            _logger.Verbose($"Keepalive message received on output endpoint: {_endpointConfig.LocalEndpointName} from {targetInstanceName}");
                            lastPongReception = DateTimeOffset.Now;
                        }
                        else if(message.IsFlushMessage())
                        {
                            await queueAccess.WaitAsync(t).ConfigureAwait(false);
                            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} handling flush message from {targetInstanceName}");
                            await dispatchQueue.EndFlush().ConfigureAwait(false);
                            await dispatchQueue.UnderlyingCollection.Writer.WriteAsync(message, t).ConfigureAwait(false); //after flush the dispatchqueue is empty, add the flush message to signal completion downstream
                            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} to {targetInstanceName} ended dispatcher queue flush, flush message sent back to downstream instance: {_endpointConfig.GetRemoteInstanceName(shardId)}");
                            queueAccess.Release();
                        }
                        else
                        {
                            _logger.Error($"Non control message received on control channel, something has gone terribly wrong.");
                            //all good, wait for the next one
                        }
                    }
                    catch (OperationCanceledException) when (pongTimeoutSource.IsCancellationRequested) //hitting this case means we have not received a keepalive before timeout
                    {
                        _logger.Warning($"KeepAlive timeout on output {_endpointConfig.LocalEndpointName} to {targetInstanceName}, initiating cancellation");
                        //there is an opening here to implement multiple timeouts before assuming actual failure.
                        //current implementation is pessimistic and instantly assumes failure.
                        pongListenerTimeoutSource.Cancel();
                        throw;
                    }
                    catch (OperationCanceledException) when (t.IsCancellationRequested) //hitting this case means the caller canceled
                    {
                        _logger.Information($"Caller cancellation on output {_endpointConfig.LocalEndpointName} to {targetInstanceName}, initiating cancellation");
                        //there is an opening here to implement multiple timeouts before assuming actual failure.
                        //current implementation is pessimistic and instantly assumes failure.
                        pongListenerTimeoutSource.Cancel();
                        throw;
                    }
                    finally
                    {
                        pongTimeoutSource?.Dispose();
                        linkedSource?.Dispose();
                    }
                }
            })
            .ContinueWith(pongTask =>
            {
                if (pongTask.IsFaulted)
                {
                    _logger.Warning(pongTask.Exception, $"Output endpoint {_endpointConfig.LocalEndpointName} to {targetInstanceName} keepalive listener exited with exception");
                }
                else if (pongTask.IsCanceled)
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} to {targetInstanceName} keepalive listener exited due to cancellation");
                }
                else
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} to {targetInstanceName} keepalive listener exited gracefully");
                }

                pongListenerTimeoutSource.Dispose();
                //pongListenerLinkedSource.Dispose();
            }, TaskScheduler.Current);

            return pongListenerTimeoutSource.Token;
        }
    }
}
