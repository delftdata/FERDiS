﻿using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Streams;
using BlackSP.Streams.Extensions;
using Nerdbank.Streams;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public class FlushableTimeoutInputEndpoint<TMessage> : IInputEndpoint, IDisposable
        where TMessage : IMessage
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate FlushableTimeoutInputEndpoint<TMessage> Factory(string endpointName);

        private readonly IReceiver<TMessage> _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public FlushableTimeoutInputEndpoint(string endpointName,
                             IVertexConfiguration vertexConfig,
                             IReceiver<TMessage> receiver,
                             ConnectionMonitor connectionMonitor,
                             ILogger logger)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));

            _ = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));
            _endpointConfig = vertexConfig.InputEndpoints.FirstOrDefault(x => x.LocalEndpointName == endpointName);

            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public async Task Ingress(Stream s, string remoteEndpointName, int remoteShardId, CancellationToken callerToken)
        {
            if (_endpointConfig.RemoteEndpointName != remoteEndpointName)
            {
                throw new Exception($"Invalid IEndpointConfig, expected remote endpointname: {_endpointConfig.RemoteEndpointName} but was: {remoteEndpointName}");
            }

            using CancellationTokenSource exceptionSource = new CancellationTokenSource();
            using CancellationTokenSource callerOrExceptionSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, exceptionSource.Token);
            Channel<byte[]> controlMsgChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Constants.DefaultThreadBoundaryQueueSize) { FullMode = BoundedChannelFullMode.Wait });

            var pipe = s.UsePipe(cancellationToken: callerToken);
            using PipeStreamReader streamReader = new PipeStreamReader(pipe.Input);
            using PipeStreamWriter streamWriter = new PipeStreamWriter(pipe.Output, true); //backchannel for control messages, should always flush
            
            var t = callerOrExceptionSource.Token;
            try
            {
                t.ThrowIfCancellationRequested();
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} starting read & deserialize threads. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.GetRemoteInstanceName(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var readThread = ReadMessagesFromStream(streamReader, remoteShardId, controlMsgChannel.Writer, callerOrExceptionSource.Token);
                var writeThread = WriteControlMessages(streamWriter, remoteShardId, controlMsgChannel.Reader, callerOrExceptionSource.Token);
                var exitedThread = await Task.WhenAny(readThread, writeThread).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
            catch (OperationCanceledException) when (t.IsCancellationRequested)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning($"Input endpoint {_endpointConfig.LocalEndpointName} read & deserialize threads ran into an exception. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.GetRemoteInstanceName(remoteShardId)}\"");
                exceptionSource.Cancel();
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
            }
        }

        private async Task ReadMessagesFromStream(PipeStreamReader streamReader, int shardId, ChannelWriter<byte[]> controlQueue, CancellationToken callerToken)
        {
            //var receptionQueue = _receiver.GetReceptionQueue(_endpointConfig, shardId);
            while (!callerToken.IsCancellationRequested)
            {
                try
                {
                    var msg = await streamReader.ReadNextMessage(callerToken).ConfigureAwait(false);
                    if (msg.IsKeepAliveMessage())
                    {
                        _logger.Verbose($"Keepalive message received on input endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName}");
                    }
                    else
                    {
                        await _receiver.Receive(msg, _endpointConfig, shardId, callerToken).ConfigureAwait(false);
                    }
                }
                catch (FlushInProgressException)
                {
                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} started flushing");
                    await controlQueue.WriteAsync(ControlMessageExtensions.ConstructFlushMessage(), callerToken);
                    byte[] msg = null;
                    while (msg == null || !msg.IsFlushMessage())
                    {
                        msg = await streamReader.ReadNextMessage(callerToken).ConfigureAwait(false); //keep taking until flush message returns from upstream
                    }
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} received flush message response");
                    await _receiver.Receive(msg, _endpointConfig, shardId, callerToken).ConfigureAwait(false);
                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} completed flushing connection with instance {_endpointConfig.GetRemoteInstanceName(shardId)}");
                }
            }
            callerToken.ThrowIfCancellationRequested();
        }


        private async Task WriteControlMessages(PipeStreamWriter streamWriter, int shardId, ChannelReader<byte[]> outputQueue, CancellationToken callerToken)
        {

            while (!callerToken.IsCancellationRequested)
            {
                using var timeoutSource = new CancellationTokenSource(Constants.KeepAliveIntervalSeconds * 1000);
                using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeoutSource.Token);
                byte[] msg;
                try
                {
                    msg = await outputQueue.ReadAsync(linkedSource.Token);
                } 
                catch(OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    msg = ControlMessageExtensions.ConstructKeepAliveMessage();

                }
                await streamWriter.WriteMessage(msg, callerToken).ConfigureAwait(false);
                var msgType = msg.IsFlushMessage() ? "flush" : "keepalive";
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} sent a {msgType} message upstream to {_endpointConfig.GetRemoteInstanceName(shardId)}");

            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
