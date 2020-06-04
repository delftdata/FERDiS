using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using Nerdbank.Streams;

namespace BlackSP.Core.Endpoints
{
    public class InputEndpoint : IInputEndpoint, IDisposable
    {
        private readonly IMessageSerializer _serializer;
        private readonly IMessageReceiver _receiver;
        private readonly IEndpointConfiguration _endpointConfig;

        public InputEndpoint(IMessageSerializer serializer,
                             IMessageReceiver receiver)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block execution, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public async Task Ingress(Stream s, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            //TODO: consider if required
            //using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(t, _messageProcessor.GetCancellationToken()))
            using (var sharedMsgQueue = new BlockingCollection<byte[]>())
            {
                var token = t;//TODO: ?? linkedTokenSource.Token;
                var exitedThread = await Task.WhenAny(
                        s.ReadMessagesTo(sharedMsgQueue, token),
                        DeserializeToReceiver(sharedMsgQueue, remoteShardId, token)
                    ).ConfigureAwait(true);
                await exitedThread.ConfigureAwait(true); //await the exited thread so any thrown exception will be rethrown
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
