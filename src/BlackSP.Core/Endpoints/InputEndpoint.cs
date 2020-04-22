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
        private ArrayPool<byte> _msgBufferPool;

        private BlockingCollection<Stream> _unprocessedMessages;
        private readonly Task _messageDeserializationThread;

        public InputEndpoint(IOperatorShell targetOperator, ISerializer serializer, ArrayPool<byte> byteArrayPool)
        {
            _operator = targetOperator ?? throw new ArgumentNullException(nameof(targetOperator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _msgBufferPool = byteArrayPool ?? throw new ArgumentNullException(nameof(byteArrayPool));

            _unprocessedMessages = new BlockingCollection<Stream>();

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
            var reader = s.UsePipeReader(0, null, t); //PipeReader.Create(s, new StreamPipeReaderOptions());
            while (!t.IsCancellationRequested) 
            {
                ReadResult readRes = await reader.ReadAsync(t);
                if(reader.TryReadMessage(readRes, out var msgbodySequence, out var bufferAfterRead))
                {
                    //and queue the message sequence for later processing
                    PrepareMessageForProcessing(msgbodySequence);
                }
                reader.AdvanceTo(bufferAfterRead.Start);
            }
        }

        private void PrepareMessageForProcessing(ReadOnlySequence<byte> msgBody)
        {
            Span<byte> msgCopy = stackalloc byte[(int)msgBody.Length];
            msgBody.CopyTo(msgCopy);
            _unprocessedMessages.Add(new MemoryStream(msgCopy.ToArray()));

        }

        private async Task DeserializeMessages(CancellationToken t)
        {
            //TODO: batch parallelize
            foreach (var msgStream in _unprocessedMessages.GetConsumingEnumerable(t))
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
                    var x = 3;
                    //throw;
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
