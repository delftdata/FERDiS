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
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public class TimeoutInputEndpoint : IInputEndpoint, IDisposable
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="endpointName"></param>
        /// <returns></returns>
        public delegate TimeoutInputEndpoint Factory(string endpointName);

        private readonly IObjectSerializer<IMessage> _serializer;
        private readonly IReceiver _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        public TimeoutInputEndpoint(string endpointName,
                             IVertexConfiguration vertexConfig,
                             IObjectSerializer<IMessage> serializer,
                             IReceiver receiver,
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
        public async Task Ingress(Stream s, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            if (_endpointConfig.RemoteEndpointName != remoteEndpointName)
            {
                throw new Exception($"Invalid IEndpointConfig, expected remote endpointname: {_endpointConfig.RemoteEndpointName} but was: {remoteEndpointName}");
            }
            BlockingCollection<byte[]> passthroughQueue = new BlockingCollection<byte[]>(1 << 12);

            try
            {
                t.ThrowIfCancellationRequested();
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName} starting read & deserialize threads. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var readerThread = Task.Run(() => ReadMessagesFromStream(s, passthroughQueue, t));
                var deserializerThread = Task.Run(() => DeserializeToReceiver(passthroughQueue, remoteShardId, t));
                var exitedTask = await Task.WhenAny(deserializerThread, readerThread).ConfigureAwait(false);
                await exitedTask.ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
            catch (OperationCanceledException) when (t.IsCancellationRequested)
            {
                _logger.Debug($"Input endpoint {_endpointConfig.LocalEndpointName} is handling cancellation request from caller side");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning($"Input endpoint {_endpointConfig.LocalEndpointName} read & deserialize threads ran into an exception. Reading from \"{_endpointConfig.RemoteVertexName} {remoteEndpointName}\" on instance \"{_endpointConfig.RemoteInstanceNames.ElementAt(remoteShardId)}\"");
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
                passthroughQueue.Dispose();
            }
        }

        private async Task ReadMessagesFromStream(Stream s, BlockingCollection<byte[]> passthroughQueue, CancellationToken t)
        {
            var pipe = s.UsePipe(cancellationToken: t);
            using PipeStreamReader streamReader = new PipeStreamReader(pipe.Input);
            using PipeStreamWriter streamWriter = new PipeStreamWriter(pipe.Output, true); //backchannel for keepalive checks, should always flush

            while (!t.IsCancellationRequested)
            {
                CancellationTokenSource timeoutSource, linkedSource;
                timeoutSource = linkedSource = null;
                try
                {
                    timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Constants.KeepAliveTimeoutSeconds));
                    linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);
                    var msg = await streamReader.ReadNextMessage(linkedSource.Token).ConfigureAwait(false);

                    if (msg.IsKeepAliveMessage())
                    {
                        _logger.Verbose($"PING received on input endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName}");
                        //respond with a the same message (keepalive)
                        await streamWriter.WriteMessage(msg, t).ConfigureAwait(false); //note we dont use the linkedsource here, the timeout did not happen so we no longer have to consider it
                        _logger.Verbose($"PONG sent on input endpoint: {_endpointConfig.LocalEndpointName} to {_endpointConfig.RemoteVertexName}");
                    }
                    else //its a regular message, queue it for processing
                    {
                        //_logger.Verbose($"Real message received deserialize queue size: {passthroughQueue.Count} - on input endpoint: {_endpointConfig.LocalEndpointName} from {_endpointConfig.RemoteVertexName}");
                        passthroughQueue.Add(msg, t);
                    }
                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                {
                    _logger.Warning($"Input endpoint {_endpointConfig.LocalEndpointName} PING timeout, throwing IOException");
                    throw new IOException($"Connection timeout, did not receive any messages for {Constants.KeepAliveTimeoutSeconds} seconds");
                }
                finally
                {
                    timeoutSource.Dispose();
                    linkedSource.Dispose();
                }
            }
            t.ThrowIfCancellationRequested();
        }

        private async Task DeserializeToReceiver(BlockingCollection<byte[]> inputqueue, int shardId, CancellationToken t)
        {
            foreach (var bytes in inputqueue.GetConsumingEnumerable(t))
            {
                IMessage message = await _serializer.DeserializeAsync(bytes, t).ConfigureAwait(false);
                if (message == null)
                {
                    throw new Exception("unexpected null message from deserializer");//TODO: custom exception?
                }
                _receiver.Receive(message, _endpointConfig, shardId);
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
