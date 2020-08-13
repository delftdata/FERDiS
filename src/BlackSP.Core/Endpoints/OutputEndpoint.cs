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
        public delegate OutputEndpoint Factory(string endpointName);

        private readonly IDispatcher<IMessage> _dispatcher;
        private readonly IVertexConfiguration _vertexConfig;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;
        
        //TODO: get ping/pong settings from environment?
        private const int KeepAliveTimeoutSeconds = 30;
        private const int KeepAliveIntervalSeconds = 10;

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
            string targetInstanceName = _endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId);
            var pipe = outputStream.UsePipe(cancellationToken: t);
            
            using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);
            using PipeStreamReader reader = new PipeStreamReader(pipe.Input);
            CancellationToken pongTimeoutToken = await StartPongListener(reader, KeepAliveTimeoutSeconds, t).ConfigureAwait(false);
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutToken);
            
            try
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting output stream writer. Writing to vertex {_endpointConfig.RemoteVertexName} on instance {targetInstanceName} on endpoint {remoteEndpointName}");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                var msgQueue = _dispatcher.GetDispatchQueue(_endpointConfig, remoteShardId);
                await StartWritingOutputWithKeepAlive(writer, msgQueue, KeepAliveIntervalSeconds, linkedSource.Token).ConfigureAwait(false);
            } 
            catch(OperationCanceledException) when(pongTimeoutToken.IsCancellationRequested)
            {
                throw new IOException($"Connection timeout, did not receive any PONG responses from {targetInstanceName} for {KeepAliveTimeoutSeconds} seconds");
            }
            catch(Exception e)
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
        private async Task StartWritingOutputWithKeepAlive(PipeStreamWriter writer, BlockingCollection<byte[]> msgQueue, int pingFrequencySeconds, CancellationToken t)
        {
            var lastPingOffset = DateTimeOffset.Now.AddSeconds(-pingFrequencySeconds);
            while (!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();

                var nextPingOffset = lastPingOffset.AddSeconds(pingFrequencySeconds);
                var timeTillNextPing = nextPingOffset - DateTimeOffset.Now;
                var msTillNextPing = (int)timeTillNextPing.TotalMilliseconds; //note truncation due to cast (basically flooring the value)
                if (msTillNextPing < 0 || !msgQueue.TryTake(out var message, msTillNextPing))
                {   //either there are no more miliseconds to wait for the next ping --> (time for another ping)
                    //or we couldnt fetch a message from the queue before those milliseconds passed --> (time for another ping)
                    message = KeepAliveExtensions.ConstructKeepAliveMessage();
                    lastPingOffset = DateTimeOffset.Now;
                    _logger.Debug($"PING sent on output endpoint: {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}");
                }

                //endpoint drops messages if dispatcher flags indicate there should not be dispatched
                //var endpointTypeDeliveryFlag = _endpointConfig.IsControl ? DispatchFlags.Control : DispatchFlags.Data;
                //if (_dispatcher.GetFlags().HasFlag(endpointTypeDeliveryFlag))
                //{ }
                
                await writer.WriteMessage(message, t).ConfigureAwait(false);

                if(message.IsKeepAliveMessage()) //ensure flushing to network to prevent buffering and timing out
                {
                    await writer.FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
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
        private async Task<CancellationToken> StartPongListener(PipeStreamReader reader, int pongTimeoutSeconds, CancellationToken t)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            //both objects get disposed in task continuation
            var pongListenerTimeoutSource = new CancellationTokenSource(); //manually cancelled after a timeout occurs
            var pongListenerLinkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongListenerTimeoutSource.Token);
#pragma warning restore CA2000 // Dispose objects before losing scope
            
            _ = Task.Run(async () =>
            {
                var lastPongReception = DateTimeOffset.Now;
                
                CancellationTokenSource pongTimeoutSource = null; //to track individual pong timeouts
                CancellationTokenSource linkedSource = null; //to track timeouts AND caller cancellation
                while (!pongListenerLinkedSource.IsCancellationRequested)
                {
                    try
                    {
                        TimeSpan timeTillNextTimeout = lastPongReception.AddSeconds(pongTimeoutSeconds) - DateTimeOffset.Now;
                        int msTillNextTimeout = (int)timeTillNextTimeout.TotalMilliseconds; //note double truncation through int cast
                        byte[] message = null;

                        pongTimeoutSource = new CancellationTokenSource(Math.Max(msTillNextTimeout, 1)); //never wait negative
                        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutSource.Token);
                        message = await reader.ReadNextMessage(linkedSource.Token).ConfigureAwait(false);
                        
                        if (!message.IsKeepAliveMessage())
                        {
                            //this is really strange we shouldnt be using the connection this way to send anything but 0 length messages (ping/pong)
                            _logger.Fatal($"Non PONG message received on PONG channel, something must have gone wrong!");
                        } 
                        else
                        {
                            _logger.Debug($"PONG received on output endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName}");
                            lastPongReception = DateTimeOffset.Now;
                            //all good, wait for the next one
                        }
                    }
                    catch (OperationCanceledException) when (pongTimeoutSource.IsCancellationRequested) //hitting this case means we have not received a pong before timeout
                    {
                        _logger.Warning($"PONG timeout on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}, initiating cancellation");
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
            }, pongListenerLinkedSource.Token)
            .ContinueWith(pongTask => 
            {
                if(pongTask.IsFaulted)
                {
                    _logger.Warning(pongTask.Exception, $"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited with exception");
                } 
                else if(pongTask.IsCanceled)
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited due to cancellation");
                }
                else
                {
                    _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} PONG listener exited gracefully");
                }

                pongListenerTimeoutSource.Dispose();
                pongListenerLinkedSource.Dispose();
            }, TaskScheduler.Current);

            return pongListenerLinkedSource.Token;
        }
    }
}
