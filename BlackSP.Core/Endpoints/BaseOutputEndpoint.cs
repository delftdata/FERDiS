﻿using BlackSP.Interfaces.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Serialization;
using System.Threading.Tasks;
using BlackSP.Interfaces.Operators;
using System.Buffers;
using Microsoft.IO;
using BlackSP.Core.Streams;
using System.Linq;

namespace BlackSP.Core.Endpoints
{

    public class BaseOutputEndpoint : IOutputEndpoint
    {
        private readonly BlockingCollection<Tuple<IEvent, OutputMode>> _outputQueue;

        //default BlockingCollection implementation is a ConcurrentQueue
        protected IDictionary<int, BlockingCollection<MemoryStream>> _shardedMessageQueues;

        protected int? _shardCount;

        private readonly ISerializer _serializer;
        private readonly IOperator _operator;
        private readonly RecyclableMemoryStreamManager _msgBufferPool;
        private readonly Task _messageSerializationThread;

        public BaseOutputEndpoint(IOperator targetOperator, ISerializer serializer, RecyclableMemoryStreamManager memStreamPool)
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

                var exitedThread = await Task.WhenAny(streamWritingThread, _messageSerializationThread);
                await exitedThread; //await the exited thread so any thrown exception will be rethrown
            }
        }

        public void Enqueue(IEvent @event, OutputMode mode)
        {
            _outputQueue.Add(new Tuple<IEvent, OutputMode>(@event, mode));
        }

        public void Enqueue(IEnumerable<IEvent> events, OutputMode mode)
        {
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
        private Task WriteMessagesToStream(Stream outputStream, int remoteShardId, CancellationToken t)
        {
            BlockingCollection<MemoryStream> msgBuffers;
            if (!_shardedMessageQueues.TryGetValue(remoteShardId, out msgBuffers))
            {
                throw new ArgumentException($"Remote shard with id {remoteShardId} has not been registered");
            }

            while (true)
            {
                t.ThrowIfCancellationRequested();
                //TODO: consider error scenario where connection closes to not lose event.. or CP?
                
                var nextMsgBuffer = msgBuffers.Take(t);
                outputStream.WriteInt32(Convert.ToInt32(nextMsgBuffer.Length));
                nextMsgBuffer.Seek(0, SeekOrigin.Begin);
                nextMsgBuffer.CopyTo(outputStream);

                nextMsgBuffer.SetLength(0);
                nextMsgBuffer.Dispose(); //return buffer to manager
            }
        }

        /// <summary>
        /// Serializes events from the outputqueue
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private async Task SerializeEvents(CancellationToken t)
        {
            while(true) //TODO: batch serialize loop
            {
                t.ThrowIfCancellationRequested();
                var nextEvent = _outputQueue.Take(t);
                
                IEvent @event = nextEvent.Item1;
                OutputMode outputMode = nextEvent.Item2;

                var msgBuffer = _msgBufferPool.GetStream();
                await _serializer.Serialize(msgBuffer, @event);
                EnqueueMessageInAppropriateShardQueue(msgBuffer, outputMode);
            }
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
                    int x = _shardCount ?? throw new ArgumentNullException("shard count not set, see: 'SetRemoteShardCount(int)'");
                    int target = 0; //TODO: hash partition function
                    var targetShardQueue = _shardedMessageQueues[target];
                    targetShardQueue.Add(msgBuffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputMode));
            }
        }
    }
}
