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

        private readonly int _pongTimeoutSeconds;
        private readonly int _pingSeconds;

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


            //TODO: get ping/pong settings from environment?
            //how frequently to ping
            _pingSeconds = 10;
            //how long to wait for the pong response
            _pongTimeoutSeconds = 30;
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

            PipeStreamWriter writer = null;
            PipeStreamReader reader = null;
            CancellationTokenSource linkedSource = null;
            try
            {
                _logger.Debug($"Output endpoint {_endpointConfig.LocalEndpointName} starting output stream writer. Writing to \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                var pipe = outputStream.UsePipe();

                var msgQueue = _dispatcher.GetDispatchQueue(_endpointConfig, remoteShardId);
                writer = new PipeStreamWriter(pipe.Output, _endpointConfig.IsControl);
                reader = new PipeStreamReader(pipe.Input);

                var pongTimeoutToken = await StartPongListener(reader, _pongTimeoutSeconds, t).ConfigureAwait(false);
                linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutToken);
                await StartWritingOutputWhilePinging(writer, msgQueue, _pingSeconds, linkedSource.Token).ConfigureAwait(false);
            } 
            catch(Exception e)
            {
                _logger.Warning(e, $"Output endpoint {_endpointConfig.LocalEndpointName} output stream writer ran into an exception. Writing to \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                throw;
            }
            finally
            {                
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
                writer?.Dispose();
                //reader?.Dispose();
                linkedSource?.Dispose();
            }
            
        }

        /// <summary>
        /// Starts writing output from provided msgQueue, inserts ping messages on the channel for keepalive-check purposes.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="msgQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task StartWritingOutputWhilePinging(PipeStreamWriter writer, BlockingCollection<byte[]> msgQueue, int pingFrequencySeconds, CancellationToken t)
        {
            var lastPingOffset = DateTimeOffset.Now.AddSeconds(-pingFrequencySeconds);
            while (!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();

                var nextPingOffset = lastPingOffset.AddSeconds(pingFrequencySeconds);
                var timeTillNextPing = nextPingOffset - DateTimeOffset.Now;
                var msTillNextPing = (int)timeTillNextPing.TotalMilliseconds; //note truncation due to cast (basically equals flooring the value)
                
                if (msTillNextPing < 0 || !msgQueue.TryTake(out var message, msTillNextPing))
                {   //either there are no more miliseconds to wait for the next ping --> (time for another ping)
                    //or we couldnt fetch a message from the queue before those milliseconds passed --> (time for another ping)
                    _logger.Verbose($"Sending out PING on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}");
                    lastPingOffset = DateTimeOffset.Now;
                    message = new byte[1] { (byte)255 }; //empty arrays are used as ping & pong messages
                }

                //endpoint drops messages if dispatcher flags indicate there should not be dispatched
                //var endpointTypeDeliveryFlag = _endpointConfig.IsControl ? DispatchFlags.Control : DispatchFlags.Data;
                //if (_dispatcher.GetFlags().HasFlag(endpointTypeDeliveryFlag))
                //{ }
                await writer.WriteMessage(message, t).ConfigureAwait(false);
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
                CancellationTokenSource linkedSource = null; //to track timeouts AND input token cancellation
                while (!pongListenerLinkedSource.IsCancellationRequested)
                {
                    try
                    {
                        TimeSpan timeTillNextTimeout = lastPongReception.AddSeconds(pongTimeoutSeconds) - DateTimeOffset.Now;
                        var msTillNextTimeout = (int)timeTillNextTimeout.TotalMilliseconds; //note double truncation through int cast

                        byte[] message = null;

                        pongTimeoutSource = new CancellationTokenSource(Math.Max(msTillNextTimeout, 1)); //never wait negative
                        linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, pongTimeoutSource.Token);
                        message = await reader.ReadNextMessage(linkedSource.Token).ConfigureAwait(false);
                        
                        if (message == null || message.Length != 1 || message[0] != (byte)255)
                        {
                            //this is really strange we shouldnt be using the connection this way to send anything but 0 length messages (ping/pong)
                            _logger.Warning($"Non PONG message received on PONG channel, something must have gone wrong!");
                            pongListenerLinkedSource.Cancel();
                        } 
                        else
                        {
                            _logger.Debug($"Received PONG on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}");
                            lastPongReception = DateTimeOffset.Now;
                            //all good, wait for the next one
                        }
                    }
                    catch (OperationCanceledException) when (pongTimeoutSource.IsCancellationRequested) //hitting this case means we have not received a pong before timeout
                    {
                        _logger.Warning($"PONG timeout on output {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}, initiating cancellation");
                        pongTimeoutSource.Dispose();
                        //there is an opening here to implement multiple timeouts before assuming actual failure.
                        //current implementation is just pessimistic and instantly assumes failure.
                        pongListenerTimeoutSource.Cancel();
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
