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

        private readonly IObjectSerializer _serializer;
        private readonly IReceiver<TMessage> _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public InputEndpoint(string endpointName,
                             IVertexConfiguration vertexConfig,
                             IObjectSerializer serializer,
                             IReceiver<TMessage> receiver,
                             ConnectionMonitor connectionMonitor,
                             ILogger logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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
            BlockingCollection<byte[]> passthroughQueue = new BlockingCollection<byte[]>(Constants.DefaultThreadBoundaryQueueSize);
            try
            {
                callerOrExceptionSource.Token.ThrowIfCancellationRequested();
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} starting read & deserialize threads. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.GetRemoteInstanceName(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);

                var pipe = s.UsePipe(cancellationToken: callerOrExceptionSource.Token);
                using PipeStreamReader streamReader = new PipeStreamReader(pipe.Input);
                using PipeStreamWriter streamWriter = new PipeStreamWriter(pipe.Output, true); //backchannel for flush requests
                var readerThread = Task.Run(() => ReadMessagesFromStream(streamReader, passthroughQueue, remoteShardId, callerOrExceptionSource.Token));
                var deserializerThread = Task.Run(() => DeserializeToReceiver(streamWriter, passthroughQueue, remoteShardId, callerOrExceptionSource.Token));
                var exitedTask = await Task.WhenAny(deserializerThread, readerThread).ConfigureAwait(false);
                await exitedTask.ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                _logger.Verbose($"Input endpoint {_endpointConfig.LocalEndpointName}${remoteShardId} is handling cancellation request from caller side");
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
                passthroughQueue.Dispose();
            }
        }

        private async Task ReadMessagesFromStream(PipeStreamReader streamReader, BlockingCollection<byte[]> passthroughQueue, int shardId, CancellationToken t)
        {
            IFlushableQueue<TMessage> receptionQueue = _receiver.GetReceptionQueue(_endpointConfig, shardId);
            while (!t.IsCancellationRequested)
            {
                var msg = await streamReader.ReadNextMessage(t).ConfigureAwait(false);
                if (msg.IsFlushMessage()) //flush message arrived from network is reply stating that flushing completed
                {
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} received flush message response");//TODO: make debug level
                    await receptionQueue.EndFlush().ConfigureAwait(false);
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} successfully ended flushing");//TODO: make debug level
                }
                passthroughQueue.Add(msg, t);
            }
            t.ThrowIfCancellationRequested();
        }

        private async Task DeserializeToReceiver(PipeStreamWriter writer, BlockingCollection<byte[]> passthroughQueue, int shardId, CancellationToken t)
        {
            var receptionQueue = _receiver.GetReceptionQueue(_endpointConfig, shardId);
            while(!t.IsCancellationRequested)
            {
                try
                {
                    if (passthroughQueue.TryTake(out var bytes, 1000))
                    {
                        //actual deserialization and adding to reception queue..
                        TMessage message = await _serializer.DeserializeAsync<TMessage>(bytes, t).ConfigureAwait(false)
                            ?? throw new Exception("unexpected null message from deserializer");//TODO: custom exception?
                        //_logger.Warning($"Input endpoint {_endpointConfig.LocalEndpointName} adding message");
                        receptionQueue.Add(message, t);
                        //_logger.Warning($"Input endpoint {_endpointConfig.LocalEndpointName} added message from {_endpointConfig.GetRemoteInstanceName(shardId)}");
                    }
                    receptionQueue.ThrowIfFlushingStarted();
                }
                catch (FlushInProgressException)
                {
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} started flushing");//TODO: make debug level
                    await writer.WriteMessage(MagicMessageExtensions.ConstructFlushMessage(), t).ConfigureAwait(false);
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} sent flush message upstream to {_endpointConfig.GetRemoteInstanceName(shardId)}");//TODO: make debug level
                    byte[] msg = null;
                    while (msg == null || !msg.IsFlushMessage())
                    {
                        msg = passthroughQueue.Take(); //keep taking until flush message returns from upstream
                    }
                    _logger.Fatal($"Input endpoint {_endpointConfig.LocalEndpointName}${shardId} completed flushing connection with instance {_endpointConfig.GetRemoteInstanceName(shardId)}");//TODO: make debug level
                }
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
