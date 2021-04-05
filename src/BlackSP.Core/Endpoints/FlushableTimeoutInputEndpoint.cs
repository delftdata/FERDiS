using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
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
    public class FlushableTimeoutInputEndpoint<TMessage> : IInputEndpoint, IDisposable
        where TMessage : IMessage
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate FlushableTimeoutInputEndpoint<TMessage> Factory(string endpointName);

        private readonly IReceiverSource<TMessage> _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public FlushableTimeoutInputEndpoint(string endpointName,
                             IVertexConfiguration vertexConfig,
                             IReceiverSource<TMessage> receiver,
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
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} from {_endpointConfig.GetRemoteInstanceName(remoteShardId)} starting ingress.");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var readThread = ReadMessagesFromStream(streamReader, remoteShardId, controlMsgChannel.Writer, callerOrExceptionSource.Token);
                var writeThread = WriteControlMessages(streamWriter, remoteShardId, controlMsgChannel.Reader, callerOrExceptionSource.Token);
                var exitedThread = await Task.WhenAny(readThread, writeThread).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
            catch (OperationCanceledException) when (t.IsCancellationRequested)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} from {_endpointConfig.GetRemoteInstanceName(remoteShardId)} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} from {_endpointConfig.GetRemoteInstanceName(remoteShardId)} ingress exited with an exception.");
                exceptionSource.Cancel();
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
            }
        }

        private async Task ReadMessagesFromStream(PipeStreamReader reader, int shardId, ChannelWriter<byte[]> controlQueue, CancellationToken t)
        {
            bool hasTakenPriority = false;
            while (!t.IsCancellationRequested)
            {
                byte[] msg = null;
                using var readTimeout = new CancellationTokenSource(2500); //let read attempt timeout after XXXms..
                using var lcts = CancellationTokenSource.CreateLinkedTokenSource(t, readTimeout.Token);
                try
                {
                    try
                    {
                        msg = msg ?? await reader.ReadNextMessage(lcts.Token).ConfigureAwait(false);
                        hasTakenPriority = await AdjustReceiverPriority(hasTakenPriority, shardId, reader.UnreadBufferFraction, 0.0d).ConfigureAwait(false); //note: deadlock odds increase greatly with every % the threshold is increased
                        await _receiver.Receive(msg, _endpointConfig, shardId, t).ConfigureAwait(false);
                        msg = null;
                    }
                    catch (OperationCanceledException) when (readTimeout.IsCancellationRequested)
                    {
                        _receiver.ThrowIfFlushInProgress(_endpointConfig, shardId);
                        //force release priority if nothing left to delivery next iteration
                        hasTakenPriority = msg == null && hasTakenPriority ? await AdjustReceiverPriority(hasTakenPriority, shardId, -1, 0.0d).ConfigureAwait(false) : hasTakenPriority;
                    }
                    catch (ReceptionCancelledException)
                    {
                        //reception was cancelled, probably to free up some critical section to allow flushing
                        //force release priority if nothing left to delivery next iteration
                        hasTakenPriority = msg == null && hasTakenPriority ? await AdjustReceiverPriority(hasTakenPriority, shardId, -1, 0.0d).ConfigureAwait(false) : hasTakenPriority;
                        //wait for retry
                        await Task.Delay(500).ConfigureAwait(false);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        //internal read message exception..
                        _logger.Warning(e, "Exception thrown by PipeStreamReader, ignoring to see what will happen next.");
                    }
                }
                catch (FlushInProgressException)
                {
                    if (hasTakenPriority)
                    {
                        //force release priority during flush
                        hasTakenPriority = await AdjustReceiverPriority(hasTakenPriority, shardId, -1, 0.0d).ConfigureAwait(false);
                    }

                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} from {_endpointConfig.GetRemoteInstanceName(shardId)} started flushing");
                    await controlQueue.WriteAsync(ControlMessageExtensions.ConstructFlushMessage(), t).ConfigureAwait(false);
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} sent flush message upstream to {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    msg = null;
                    while (msg == null || !msg.IsFlushMessage())
                    {
                        msg = await reader.ReadNextMessage(t).ConfigureAwait(false); //keep reading&discarding until flush message returns from upstream
                    }
                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} received flush message response from {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    _receiver.CompleteFlush(_endpointConfig, shardId);
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} completed flushing connection with upstream instance {_endpointConfig.GetRemoteInstanceName(shardId)}");
                }
            }
            t.ThrowIfCancellationRequested();
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

        /// <summary>
        /// Local subroutine that takes or releases priority with the receiver depending on the amount of unread data in the provided buffer
        /// </summary>
        /// <param name="hadPriority"></param>
        /// <param name="shardId"></param>
        /// <param name="unreadBufferFraction"></param>
        /// <returns></returns>
        private async Task<bool> AdjustReceiverPriority(bool hadPriority, int shardId, double unreadBufferFraction, double priorityThreshold)
        {
            bool needsPriority = _endpointConfig.IsBackchannel && unreadBufferFraction > priorityThreshold; //hand priority to backchannels to prevent distributed deadlocks
            if (!hadPriority && needsPriority)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is taking priority, capacity: {unreadBufferFraction:F2}");
                await _receiver.TakePriority(_endpointConfig, shardId).ConfigureAwait(false);
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has taken priority, capacity: {unreadBufferFraction:F2}");
            }
            if (hadPriority && !needsPriority)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is releasing priority, capacity: {unreadBufferFraction:F2}");
                _receiver.ReleasePriority(_endpointConfig, shardId);
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has released priority, capacity: {unreadBufferFraction:F2}");
            }
            return needsPriority;
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
