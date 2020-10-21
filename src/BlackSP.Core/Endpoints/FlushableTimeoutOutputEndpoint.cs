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
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public FlushableTimeoutOutputEndpoint(string endpointName,
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
            string targetInstanceName = _endpointConfig.GetRemoteInstanceName(remoteShardId);

            using CancellationTokenSource exceptionSource = new CancellationTokenSource();
            using CancellationTokenSource callerOrExceptionSource = CancellationTokenSource.CreateLinkedTokenSource(exceptionSource.Token, callerToken);
            var pipe = outputStream.UsePipe(cancellationToken: callerOrExceptionSource.Token);
            
            using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);
            using PipeStreamReader reader = new PipeStreamReader(pipe.Input);
            using SemaphoreSlim queueAccess = new SemaphoreSlim(1, 1);

            CancellationToken pongTimeoutToken = StartControlMessageListener(reader, remoteShardId, queueAccess, callerOrExceptionSource.Token);
            using CancellationTokenSource callerExceptionOrTimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(callerOrExceptionSource.Token, pongTimeoutToken);

            try
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting output stream writer. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                await WriteDispatchableMessages(writer, remoteShardId, queueAccess, callerExceptionOrTimeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} is handling cancellation request from caller side");
                throw;
            }
            catch (OperationCanceledException) when (pongTimeoutToken.IsCancellationRequested)
            {
                _logger.Warning($"Output endpoint {_endpointConfig.LocalEndpointName} PONG-reception timeout, throwing IOException");
                exceptionSource.Cancel();
                throw new IOException($"Connection timeout, did not receive any PONG responses from {targetInstanceName} for {Constants.KeepAliveTimeoutSeconds} seconds");
            }
            catch (Exception e)
            {
                exceptionSource.Cancel();
                _logger.Warning($"Output endpoint {_endpointConfig.LocalEndpointName} output stream writer ran into an exception. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");
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
            while (!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();
                await queueAccess.WaitAsync(t).ConfigureAwait(false);
                if (dispatchQueue.TryTake(out var message, 1000, t))
                {
                    await writer.WriteMessage(message, t).ConfigureAwait(false);
                    if (message.IsFlushMessage())
                    {
                        await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                    }
                } 
                else
                {
                    //there was no message to dispatch before trytake timeout
                    //flush whatever is still in the output buffer
                    await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
                }
                queueAccess.Release();
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
#pragma warning disable CA2000 // Dispose objects before losing scope
            //object gets disposed in task continuation
            var pongListenerTimeoutSource = new CancellationTokenSource(); //manually cancelled after a timeout occurs
#pragma warning restore CA2000 // Dispose objects before losing scope

            _ = Task.Run(async () =>
            {
                var dispatchQueue = _dispatcher.GetDispatchQueue(_endpointConfig, shardId);
                var lastPongReception = DateTimeOffset.Now;

                CancellationTokenSource pongTimeoutSource = null; //to track individual pong timeouts
                CancellationTokenSource linkedSource = null; //to track timeouts AND caller cancellation
                while (true)
                {
                    t.ThrowIfCancellationRequested();
                    try
                    {
                        TimeSpan timeTillNextTimeout = lastPongReception.AddSeconds(Constants.KeepAliveTimeoutSeconds) - DateTimeOffset.Now;
                        int msTillNextTimeout = (int)timeTillNextTimeout.TotalMilliseconds; //note double truncation through int cast

                        pongTimeoutSource = new CancellationTokenSource(Math.Max(msTillNextTimeout, 1)); //never wait negative
                        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutSource.Token);
                        _logger.Verbose($"awaiting control message on output endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName} within {msTillNextTimeout}ms");

                        var message = await reader.ReadNextMessage(linkedSource.Token).ConfigureAwait(false);
                        if (message.IsKeepAliveMessage())
                        {
                            _logger.Verbose($"Keepalive message received on output endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName}");
                            lastPongReception = DateTimeOffset.Now;
                        }
                        else if(message.IsFlushMessage())
                        {
                            await queueAccess.WaitAsync(t).ConfigureAwait(false);
                            _logger.Verbose($"Output endpoint {_endpointConfig.LocalEndpointName} handling flush message from {_endpointConfig.GetRemoteInstanceName(shardId)}");
                            await dispatchQueue.EndFlush().ConfigureAwait(false);
                            dispatchQueue.Add(message, t); //after flush the dispatchqueue is empty, add the flush message to signal completion downstream
                            _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} ended dispatcher queue flush, flush message sent back to downstream instance: {_endpointConfig.GetRemoteInstanceName(shardId)}");
                            queueAccess.Release();
                        }
                        else
                        {
                            _logger.Error($"Non control message received on control channel, something has gone terribly wrong.");
                            //all good, wait for the next one
                        }
                    }
                    catch (OperationCanceledException) when (pongTimeoutSource.IsCancellationRequested) //hitting this case means we have not received a pong before timeout
                    {
                        _logger.Warning($"Hearbeat timeout on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}, initiating cancellation");
                        //there is an opening here to implement multiple timeouts before assuming actual failure.
                        //current implementation is just pessimistic and instantly assumes failure.
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
                    _logger.Warning(pongTask.Exception, $"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited with exception");
                }
                else if (pongTask.IsCanceled)
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited due to cancellation");
                }
                else
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited gracefully");
                }

                pongListenerTimeoutSource.Dispose();
                //pongListenerLinkedSource.Dispose();
            }, TaskScheduler.Current);

            return pongListenerTimeoutSource.Token;
        }
    }
}
