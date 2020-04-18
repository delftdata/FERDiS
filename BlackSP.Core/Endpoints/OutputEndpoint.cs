using BlackSP.Kernel.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Serialization;
using System.Threading.Tasks;
using BlackSP.Kernel.Operators;
using System.Buffers;
using Microsoft.IO;
using BlackSP.Core.Extensions;
using System.Linq;
using Nerdbank.Streams;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint, IDisposable
    {
        private readonly BlockingCollection<Tuple<IEvent, OutputMode>> _outputQueue;
        private readonly IDictionary<int, BlockingCollection<MemoryStream>> _shardedMessageQueues; //default BlockingCollection implementation is a ConcurrentQueue
        private readonly ISerializer _serializer;
        private readonly IOperatorSocket _operator;
        private readonly RecyclableMemoryStreamManager _msgBufferPool;
        private readonly Task _messageSerializationThread;
        private int? _shardCount;

        public OutputEndpoint(IOperatorSocket targetOperator, ISerializer serializer, RecyclableMemoryStreamManager memStreamPool)
        {
            _operator = targetOperator ?? throw new ArgumentNullException(nameof(targetOperator));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _msgBufferPool = memStreamPool ?? throw new ArgumentNullException(nameof(memStreamPool));

            _outputQueue = new BlockingCollection<Tuple<IEvent, OutputMode>>();
            _shardedMessageQueues = new ConcurrentDictionary<int, BlockingCollection<MemoryStream>>();

            //The message serialization thread will only die with the output endpoint itself
            //this should only happen when the operator crashes (if the runtime gets killed we dont care anyway)
            _messageSerializationThread = Task.Run(() => SerializeEvents(_operator.CancellationToken));
            _operator.RegisterOutputEndpoint(this);
        }

        /// <summary>
        /// Starts a blocking loop that will check the 
        /// registered remote shard's output queue for
        /// new events and write them to the provided
        /// outputstream.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        public async Task Egress(Stream outputStream, int remoteShardId, CancellationToken t)
        {
            //cancels when launching thread requests cancel or when operator requests cancel
            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(t, _operator.CancellationToken))
            {
                var token = linkedTokenSource.Token;
                var streamWritingThread = Task.Run(() => WriteMessagesToStream(outputStream, remoteShardId, token));

                var exitedThread = await Task.WhenAny(streamWritingThread, _messageSerializationThread).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false); //await the exited thread so any thrown exception will be rethrown
            }
        }

        /// <summary>
        /// Enqueue event for egressing
        /// </summary>
        /// <param name="event"></param>
        /// <param name="mode"></param>
        public void Enqueue(IEvent @event, OutputMode mode)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            _outputQueue.Add(new Tuple<IEvent, OutputMode>(@event, mode));
        }

        /// <summary>
        /// Enqueue events for egressing
        /// </summary>
        /// <param name="events"></param>
        /// <param name="mode"></param>
        public void Enqueue(IEnumerable<IEvent> events, OutputMode mode)
        {
            _ = events ?? throw new ArgumentNullException(nameof(events));
            foreach (var @event in events)
            {
                Enqueue(@event, mode);
            }
        }

        /// <summary>
        /// Used in partitioning output over shards
        /// </summary>
        /// <param name="shardCount"></param>
        public void SetRemoteShardCount(int shardCount)
        {
            _shardCount = shardCount;
        }

        /// <summary>
        /// Registers a remote shard with given id.
        /// An outputqueue is provisioned to be used
        /// during Egress
        /// </summary>
        /// <param name="remoteShardId"></param>
        /// <returns></returns>
        public bool RegisterRemoteShard(int remoteShardId)
        {
            if (_shardedMessageQueues.ContainsKey(remoteShardId))
            {
                return false;
            }
            _shardedMessageQueues.Add(remoteShardId, new BlockingCollection<MemoryStream>());
            return true;
        }

        /// <summary>
        /// Unregisters a remote shard with given id
        /// </summary>
        /// <param name="remoteShardId"></param>
        /// <returns></returns>
        public bool UnregisterRemoteShard(int remoteShardId)
        {
            return _shardedMessageQueues.Remove(remoteShardId);
        }

        /// <summary>
        /// Dequeues serialized messages from the appropriate shard queue
        /// and copies the contents with length prefixed to the output stream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task WriteMessagesToStream(Stream outputStream, int remoteShardId, CancellationToken t)
        {
            if (!_shardedMessageQueues.TryGetValue(remoteShardId, out BlockingCollection<MemoryStream> msgBuffers))
            {
                throw new ArgumentException($"Remote shard with id {remoteShardId} has not been registered");
            }
            var writer = outputStream.UsePipeWriter();
            var writtenBytes = 0;
            var currentWriteBuffer = writer.GetMemory();
            foreach(var nextMsgBuffer in msgBuffers.GetConsumingEnumerable(t))
            {
                int nextMsgLength = Convert.ToInt32(nextMsgBuffer.Length);
                Memory<byte> nextMsgLengthBytes = BitConverter.GetBytes(nextMsgLength).AsMemory();
                Memory<byte> nextMsgBodyBytes = nextMsgBuffer.GetBuffer().AsMemory().Slice(0, nextMsgLength);
                
                //reserve at least enough space for the actual message + 4 bytes for the leading size integer
                int bytesToWrite = nextMsgLength + 4;
                
                if(writtenBytes + bytesToWrite > currentWriteBuffer.Length)
                {
                    //buffer will overflow, flush first
                    writer.Advance(writtenBytes);
                    await writer.FlushAsync();
                    writtenBytes = 0;

                    currentWriteBuffer = writer.GetMemory(bytesToWrite);
                }                
                
                var msgLengthBufferSegment = currentWriteBuffer.Slice(writtenBytes, 4);
                nextMsgLengthBytes.CopyTo(msgLengthBufferSegment);

                var msgBodyBufferSegment = currentWriteBuffer.Slice(writtenBytes + 4, nextMsgLength);
                nextMsgBodyBytes.CopyTo(msgBodyBufferSegment);

                writtenBytes += bytesToWrite;
                
                //finally dipose of msg buffer (it came from the memstream manager)
                nextMsgBuffer.SetLength(0);
                nextMsgBuffer.Dispose();
            }

            t.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Serializes events from the outputqueue
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task SerializeEvents(CancellationToken t)
        {
            while(!t.IsCancellationRequested) //TODO: batch parallelize loop
            {
                var nextTuple = _outputQueue.Take(t);
                
                IEvent @event = nextTuple.Item1;
                OutputMode outputMode = nextTuple.Item2;

                var msgBuffer = _msgBufferPool.GetStream();
                await _serializer.Serialize(msgBuffer, @event).ConfigureAwait(false);
                EnqueueMessageInAppropriateShardQueue(msgBuffer, outputMode);
            }
            t.ThrowIfCancellationRequested();
        }

        private void EnqueueMessageInAppropriateShardQueue(MemoryStream msgBuffer, OutputMode outputMode)
        {
            switch (outputMode)
            {
                case OutputMode.Broadcast:
                    //strategy: copy stream for each shard
                    //why: avoid risk of two threads trying to dispose the same stream, making it unavailable to the other
                    Parallel.ForEach(_shardedMessageQueues, (kvShard) =>
                    {   //TODO: consider sequentializing and using msgBuffer for last shardQueue (1 less copy)
                        var shardQueue = kvShard.Value;
                        var bufferCopy = _msgBufferPool.GetStream(null, Convert.ToInt32(msgBuffer.Length));
                        msgBuffer.Seek(0, SeekOrigin.Begin);
                        msgBuffer.CopyTo(bufferCopy);
                        shardQueue.Add(bufferCopy);
                    });
                    msgBuffer.Dispose(); //return buffer to recyclemanager
                    break;
                case OutputMode.Partition:
                    string key = "";
                    
                    int x = _shardCount ?? throw new ArgumentNullException("shard count not set, see: 'SetRemoteShardCount(int)'");
                    int target = 0; //TODO: hash partition function
                    var targetShardQueue = _shardedMessageQueues[target];
                    targetShardQueue.Add(msgBuffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputMode));
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
                    _outputQueue.Dispose();
                    _messageSerializationThread.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

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
