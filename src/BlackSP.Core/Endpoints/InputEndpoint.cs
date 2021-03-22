using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Streams;
using BlackSP.Streams.Extensions;
using Nerdbank.Streams;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public class InputEndpoint<TMessage> : IInputEndpoint, IDisposable
        where TMessage : IMessage
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate InputEndpoint<TMessage> Factory(string endpointName);

        private readonly IReceiverSource<TMessage> _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public InputEndpoint(string endpointName,
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
            if(_endpointConfig.RemoteEndpointName != remoteEndpointName)
            {
                throw new Exception($"Invalid IEndpointConfig, expected remote endpointname: {_endpointConfig.RemoteEndpointName} but was: {remoteEndpointName}");
            }

            using CancellationTokenSource exceptionSource = new CancellationTokenSource();
            using CancellationTokenSource callerOrExceptionSource = CancellationTokenSource.CreateLinkedTokenSource(callerToken, exceptionSource.Token);
            try
            {
                callerOrExceptionSource.Token.ThrowIfCancellationRequested();
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} starting read & deserialize threads. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.GetRemoteInstanceName(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                IDuplexPipe pipe = s.UsePipe(cancellationToken: callerOrExceptionSource.Token);
                await ReceiveMessages(pipe, remoteShardId, callerOrExceptionSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName} from {_endpointConfig.GetRemoteInstanceName(remoteShardId)} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} read & deserialize threads ran into an exception. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.GetRemoteInstanceName(remoteShardId)}\"");
                exceptionSource.Cancel();
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
            }
        }

        private async Task ReceiveMessages(IDuplexPipe pipe, int shardId, CancellationToken t)
        {
            using PipeStreamReader reader = new PipeStreamReader(pipe.Input);
            using PipeStreamWriter writer = new PipeStreamWriter(pipe.Output, true); //backchannel for flush requests

            bool hasDeliveredToReceiver = false;
            bool hasTakenPriority = false;
            byte[] msg = null;
            while (!t.IsCancellationRequested)
            {
                CancellationTokenSource timeoutSrc = new CancellationTokenSource(5000); //to auto-cancel and retry with flush checks
                CancellationTokenSource lcts = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSrc.Token);
                try
                {
                    _receiver.ThrowIfFlushInProgress(_endpointConfig, shardId);
                    if(!hasDeliveredToReceiver) //may be retry where message consumption must be awaited first
                    {
                        //timeoutSrc = new CancellationTokenSource(5000);
                        //lcts = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSrc.Token);


                        msg = msg ?? await reader.ReadNextMessage(lcts.Token).ConfigureAwait(false);
                        hasTakenPriority = await AdjustReceiverPriority(hasTakenPriority, shardId, reader.UnreadBufferFraction, 0.0d).ConfigureAwait(false); //note: deadlock odds increase greatly with every % the threshold is increased
                        await _receiver.Receive(msg, _endpointConfig, shardId, lcts.Token).ConfigureAwait(false);

                        msg = null;
                        hasDeliveredToReceiver = true;
                    }
                    
                    _receiver.ThrowIfFlushInProgress(_endpointConfig, shardId);
                    if (hasDeliveredToReceiver)
                    {
                        //timeoutSrc?.Dispose();
                        //lcts?.Dispose();

                        await _receiver.WaitForNext(_endpointConfig, shardId, lcts.Token).ConfigureAwait(false);
                        hasDeliveredToReceiver = false;
                    }
                }
                catch (OperationCanceledException) when (timeoutSrc.IsCancellationRequested)
                {
                    //retry loop to check if delivery preconditions changed (note how msg read could be skipped)
                    continue;
                }
                catch (FlushInProgressException)
                {
                    if(hasTakenPriority)
                    {
                        //force release priority during flush
                        hasTakenPriority = await AdjustReceiverPriority(hasTakenPriority, shardId, -1, 0.0d).ConfigureAwait(false);
                    }

                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} started flushing");
                    await writer.WriteMessage(ControlMessageExtensions.ConstructFlushMessage(), t).ConfigureAwait(false);
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} sent flush message upstream to {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    byte[] fmsg = null;
                    while (fmsg == null || !fmsg.IsFlushMessage())
                    {
                        fmsg = await reader.ReadNextMessage(t).ConfigureAwait(false); //keep reading&discarding until flush message returns from upstream
                    }
                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} received flush message response");
                    _receiver.CompleteFlush(_endpointConfig, shardId);
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} completed flushing connection with instance {_endpointConfig.GetRemoteInstanceName(shardId)}");

                    //lastly reset local variables
                    hasDeliveredToReceiver = false;
                    msg = null;
                }
                finally
                {
                    timeoutSrc.Dispose();
                    lcts.Dispose();
                }
            }
            t.ThrowIfCancellationRequested();
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
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is taking priority, capacity: {unreadBufferFraction:F2}");
                await _receiver.TakePriority(_endpointConfig, shardId).ConfigureAwait(false);
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has taken priority, capacity: {unreadBufferFraction:F2}");
            }
            if (hadPriority && !needsPriority)
            {
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is releasing priority, capacity: {unreadBufferFraction:F2}");
                _receiver.ReleasePriority(_endpointConfig, shardId);
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has released priority, capacity: {unreadBufferFraction:F2}");
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
