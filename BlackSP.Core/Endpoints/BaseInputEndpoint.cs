using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Streams;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Operators;
using BlackSP.Interfaces.Serialization;

namespace BlackSP.Core.Endpoints
{
    public class BaseInputEndpoint : IInputEndpoint
    {
        private IOperator _operator;
        private ISerializer _serializer;
        private ArrayPool<byte> _msgBufferPool;

        private ConcurrentQueue<IEvent> _inputQueue;
        private BlockingCollection<byte[]> _unprocessedMessages;

        public BaseInputEndpoint(IOperator targetOperator, ISerializer serializer, ArrayPool<byte> byteArrayPool)
        {
            _operator = targetOperator ?? throw new ArgumentNullException(nameof(targetOperator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _msgBufferPool = byteArrayPool ?? throw new ArgumentNullException(nameof(byteArrayPool));

            _inputQueue = new ConcurrentQueue<IEvent>();
            _unprocessedMessages = new BlockingCollection<byte[]>(); 
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
                var messageProcessingThread = Task.Run(() => ProcessMessages(token));

                var exitedThread = await Task.WhenAny(streamReadingThread, messageProcessingThread);
                await exitedThread; //await the exited thread so any thrown exception will be rethrown
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
            {
                int nextMsgLength = await s.ReadInt32Async();
                if (nextMsgLength <= 0) { continue; }

                byte[] buffer = _msgBufferPool.Rent(nextMsgLength);
                int realMsgLength = await s.ReadAllRequiredBytesAsync(buffer, 0, nextMsgLength);
                if (nextMsgLength != realMsgLength)
                {
                    //TODO: log/throw?
                    _msgBufferPool.Return(buffer); //gotta return the buffer in this case
                }
                else
                {
                    _unprocessedMessages.Add(buffer);
                }
            }
        }

        private async Task ProcessMessages(CancellationToken t)
        {
            while(!t.IsCancellationRequested)
            {
                //TODO: batch parallelize
                byte[] nextMsgBuffer = _unprocessedMessages.Take(t);
                var msgStream = new MemoryStream(nextMsgBuffer);
                try
                {
                    var nextEvent = await _serializer.Deserialize<IEvent>(msgStream, t);
                    if (nextEvent == null)
                    {
                        //TODO: log/throw?
                        continue;
                    }
                    _inputQueue.Enqueue(nextEvent); //TODO enqueue in operator
                } 
                finally
                {
                    _msgBufferPool.Return(nextMsgBuffer);
                    msgStream.Dispose();
                }
            }
        }

        public bool HasInput()
        {
            return !_inputQueue.IsEmpty;
        }

        public IEvent GetNext()
        {
            return _inputQueue.TryDequeue(out IEvent result) ? result : null;
        }

    }
}
