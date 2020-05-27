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
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using Nerdbank.Streams;

namespace BlackSP.Core.Endpoints
{
    public class InputEndpoint : IInputEndpoint, IDisposable
    {
        private IOperatorShell _operator;
        private ISerializer _serializer;

        public InputEndpoint(IOperatorShell targetOperator, ISerializer serializer)
        {
            _operator = targetOperator ?? throw new ArgumentNullException(nameof(targetOperator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block execution, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public async Task Ingress(Stream s, CancellationToken t)
        {
            //cancels when launching thread requests cancel or when operator requests cancel
            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(t, _operator.CancellationToken))
            using (var sharedMsgQueue = new BlockingCollection<Stream>())
            {
                var token = linkedTokenSource.Token;
                var exitedThread = await Task.WhenAny(
                        ReadMessagesFromStream(s, sharedMsgQueue, token), 
                        DeserializeMessages(sharedMsgQueue, token)
                    ).ConfigureAwait(true);
                
                await exitedThread.ConfigureAwait(true); //await the exited thread so any thrown exception will be rethrown
            }
        }

        /// <summary>
        /// Starts reading from provided stream and storing byte[]s of the messages in this local queue.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task ReadMessagesFromStream(Stream s, BlockingCollection<Stream> deserializationQueue, CancellationToken t)
        {
            var reader = s.UsePipeReader(0, null, t);
            
            ReadResult readRes = await reader.ReadAsync(t);
            var currentBuffer = readRes.Buffer;
            while (!t.IsCancellationRequested) 
            {
                if(currentBuffer.TryReadMessage(out var msgbodySequence, out var readPosition))
                {
                    deserializationQueue.Add(msgbodySequence.ToStream());
                    currentBuffer = currentBuffer.Slice(readPosition);
                }
                else
                {
                    reader.AdvanceTo(readPosition);
                    readRes = await reader.ReadAsync(t);
                    currentBuffer = readRes.Buffer;
                }
            }
        }

        /// <summary>
        /// Enters blocking loop that consumes from blocking collection and deserializes the data in the streams
        /// </summary>
        /// <param name="deserializationQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task DeserializeMessages(BlockingCollection<Stream> deserializationQueue, CancellationToken t)
        {
            foreach (var msgStream in deserializationQueue.GetConsumingEnumerable(t))
            {
                try
                {
                    var nextEvent = await _serializer.Deserialize<IEvent>(msgStream, t).ConfigureAwait(false);
                    if (nextEvent == null)
                    {
                        //TODO: log/throw?
                        Console.WriteLine("This absolutely shouldnt happen");
                        continue;
                    }
                    _operator.Enqueue(nextEvent);
                } 
                catch(Exception e)
                {
                    throw;
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
