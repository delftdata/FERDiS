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

        private readonly IReceiver<TMessage> _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public InputEndpoint(string endpointName,
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

            bool hasTakenPriority = false;
            byte[] msg = null;
            while (!t.IsCancellationRequested)
            {
                CancellationTokenSource timeoutSrc = null;
                CancellationTokenSource lcts = null;
                try
                {
                    timeoutSrc = new CancellationTokenSource(5000);
                    lcts = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSrc.Token);

                    _receiver.ThrowIfReceivePreconditionsNotMet(_endpointConfig, shardId);
                    msg = msg ?? await reader.ReadNextMessage(lcts.Token).ConfigureAwait(false);

                    //EXTRACT METHOD
                    bool needsPriority = _endpointConfig.IsBackchannel && reader.UnreadBufferFraction > 0.1d; //aggressively hand priority to backchannels to prevent distributed deadlocks
                    if (!hasTakenPriority && needsPriority)
                    {
                        _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is taking priority, capacity: {reader.UnreadBufferFraction:F2}");
                        await _receiver.TakePriority(_endpointConfig, shardId).ConfigureAwait(false);
                        _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has taken priority, capacity: {reader.UnreadBufferFraction:F2}");
                    }
                    if (hasTakenPriority && !needsPriority)
                    {
                        _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} is releasing priority, capacity: {reader.UnreadBufferFraction:F2}");
                        _receiver.ReleasePriority(_endpointConfig, shardId);
                        _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} has released priority, capacity: {reader.UnreadBufferFraction:F2}");
                    }
                    //EXTRACT M<ETHOD
                    hasTakenPriority = needsPriority;
                    
                    await _receiver.Receive(msg, _endpointConfig, shardId, lcts.Token).ConfigureAwait(false);
                    msg = null;
                }
                catch(OperationCanceledException) when (timeoutSrc.IsCancellationRequested)
                {
                    //retry loop to check if delivery preconditions changed
                    continue;
                }
                catch (FlushInProgressException)
                {
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} started flushing");
                    await writer.WriteMessage(ControlMessageExtensions.ConstructFlushMessage(), t).ConfigureAwait(false);
                    _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} sent flush message upstream to {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    byte[] fmsg = null;
                    while (fmsg == null || !fmsg.IsFlushMessage())
                    {
                        fmsg = await reader.ReadNextMessage(t).ConfigureAwait(false); //keep reading&discarding until flush message returns from upstream
                    }
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} received flush message response");
                    await _receiver.Receive(fmsg, _endpointConfig, shardId, t).ConfigureAwait(false);
                    _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} completed flushing connection with instance {_endpointConfig.GetRemoteInstanceName(shardId)}");
                }
                finally
                {
                    timeoutSrc.Dispose();
                    lcts.Dispose();
                }
            }
            t.ThrowIfCancellationRequested();
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
