using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Extensions;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Streams.Extensions;

namespace BlackSP.Core.Endpoints
{
    public class InputEndpoint : IInputEndpoint, IDisposable
    {

        public delegate InputEndpoint Factory(string endpointName);

        private readonly IMessageSerializer _serializer;
        private readonly IReceiver _receiver;
        private readonly IEndpointConfiguration _endpointConfig;
        private readonly ConnectionMonitor _connectionMonitor;

        public InputEndpoint(string endpointName,
                             IMessageSerializer serializer,
                             IReceiver receiver,
                             IVertexConfiguration vertexConfig,
                             ConnectionMonitor connectionMonitor)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));

            _ = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));
            _endpointConfig = vertexConfig.InputEndpoints.FirstOrDefault(x => x.LocalEndpointName == endpointName);

            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public async Task Ingress(Stream s, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            if(_endpointConfig.RemoteEndpointName != remoteEndpointName)
            {
                throw new Exception($"Invalid IEndpointConfig, expected remote endpointname: {_endpointConfig.RemoteEndpointName} but was: {remoteEndpointName}");
            }
            BlockingCollection<byte[]> sharedMsgQueue = new BlockingCollection<byte[]>(64);
            try
            {
                t.ThrowIfCancellationRequested();
                _connectionMonitor.MarkConnected(_endpointConfig, remoteShardId);
                var deserializerThread = Task.Run(() => DeserializeToReceiver(sharedMsgQueue, remoteShardId, t));
                await await Task.WhenAny(deserializerThread, s.ReadMessagesTo(sharedMsgQueue, t)).ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
            catch(Exception e)
            {
                throw;
            }
            finally
            {
                _connectionMonitor.MarkDisconnected(_endpointConfig, remoteShardId);
                sharedMsgQueue.Dispose();
            }
        }

        private async Task DeserializeToReceiver(BlockingCollection<byte[]> inputqueue, int shardId, CancellationToken t)
        {
            foreach(var bytes in inputqueue.GetConsumingEnumerable(t))
            {
                IMessage message = await _serializer.DeserializeMessage(bytes, t).ConfigureAwait(false);
                if(message == null)
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
