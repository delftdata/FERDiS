using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Streams;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;

namespace BlackSP.Core.Endpoints
{
    public class InputEndpoint : IInputEndpoint, IDisposable
    {
        private IOperatorSocket _operator;
        private ISerializer _serializer;
        private ArrayPool<byte> _msgBufferPool;

        private BlockingCollection<Tuple<int, byte[]>> _unprocessedMessages;
        private readonly Task _messageDeserializationThread;

        public InputEndpoint(IOperatorSocket targetOperator, ISerializer serializer, ArrayPool<byte> byteArrayPool)
        {
            _operator = targetOperator ?? throw new ArgumentNullException(nameof(targetOperator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _msgBufferPool = byteArrayPool ?? throw new ArgumentNullException(nameof(byteArrayPool));

            _unprocessedMessages = new BlockingCollection<Tuple<int, byte[]>>();

            _messageDeserializationThread = Task.Run(() => DeserializeMessages(_operator.CancellationToken));
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
            {
                var token = linkedTokenSource.Token;
                var streamReadingThread = Task.Run(() => ReadMessagesFromStream(s, token));

                var exitedThread = await Task.WhenAny(streamReadingThread, _messageDeserializationThread).ConfigureAwait(true);
                await exitedThread.ConfigureAwait(true); //await the exited thread so any thrown exception will be rethrown
            }
        }

        /// <summary>
        /// Starts reading from provided stream and storing byte[]s of the messages in this local queue.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task ReadMessagesFromStream(Stream s, CancellationToken t)
        {
            while (!t.IsCancellationRequested) 
                //this must be the bottleneck, loop bashes s.read causing the immense cpu usage
            {
                int nextMsgLength = await s.ReadInt32Async().ConfigureAwait(false);
                if (nextMsgLength <= 0) { continue; }

                byte[] buffer = _msgBufferPool.Rent(nextMsgLength);
                int realMsgLength = await s.ReadAllRequiredBytesAsync(buffer, 0, nextMsgLength).ConfigureAwait(false);
                
                if (nextMsgLength != realMsgLength)
                {
                    //TODO: log/throw?
                    Console.WriteLine("This shouldnt happen");
                    _msgBufferPool.Return(buffer); //gotta return the buffer due to error
                }
                else
                {
                    _unprocessedMessages.Add(Tuple.Create(nextMsgLength, buffer));
                }
            }
        }

        private async Task DeserializeMessages(CancellationToken t)
        {
            while(!t.IsCancellationRequested)
            {
                //TODO: batch parallelize
                var tuple = _unprocessedMessages.Take(t);
                int nextMsgLength = tuple.Item1;
                byte[] nextMsgBuffer = tuple.Item2;
                var msgStream = new MemoryStream(nextMsgBuffer, 0, nextMsgLength); //TODO: check memory usage
                try
                {
                    var nextEvent = await _serializer.Deserialize<IEvent>(msgStream, t).ConfigureAwait(true);
                    if (nextEvent == null)
                    {
                        //TODO: log/throw?
                        continue;
                    }
                    _operator.Enqueue(nextEvent);
                } 
                /*catch(Exception e)
                {
                    //throw;
                    Console.WriteLine("oops");
                }*/
                finally
                {
                    _msgBufferPool.Return(nextMsgBuffer);
                    msgStream.Dispose();
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
                    _messageDeserializationThread.Dispose();
                    _unprocessedMessages.Dispose();
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
