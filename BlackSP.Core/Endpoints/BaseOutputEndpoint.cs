using BlackSP.Interfaces.Events;
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

namespace BlackSP.Core.Endpoints
{

    public class BaseOutputEndpoint : IOutputEndpoint
    {
        private readonly BlockingCollection<Tuple<IEvent, OutputMode>> _outputQueue;

        //default BlockingCollection implementation is a ConcurrentQueue
        protected IDictionary<int, BlockingCollection<MemoryStream>> _shardedMessageQueues;

        protected int _shardCount;

        private readonly ISerializer _serializer;
        private readonly IOperator _operator;
        private readonly RecyclableMemoryStreamManager _msgBufferPool;

        private readonly Task _messageSerializationThread;

        public BaseOutputEndpoint(IOperator targetOperator, ISerializer serializer, RecyclableMemoryStreamManager memStreamPool)
        {
            _operator = targetOperator;//todo: throw
            _serializer = serializer;//todo: throw
            _msgBufferPool = memStreamPool;//todo: throw

            _outputQueue = new BlockingCollection<Tuple<IEvent, OutputMode>>();

            _shardCount = 0;
            _shardedMessageQueues = new ConcurrentDictionary<int, BlockingCollection<MemoryStream>>();

            //The message serialization thread will only die with the output endpoint itself
            //this only happens when the operator crashes (and if the runtime gets killed we dont care anwyay)
            _messageSerializationThread = Task.Run(() => PrepareMessages(_operator.CancellationToken));
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
            if(_shardedMessageQueues.ContainsKey(remoteShardId))
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

        private async Task WriteMessagesToStream(Stream outputStream, int remoteShardId, CancellationToken t)
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
                nextMsgBuffer.CopyTo(outputStream);

                nextMsgBuffer.SetLength(0); //TODO: check if should do before or after serializing
                nextMsgBuffer.Dispose(); //return buffer to memory manager
            }
        }

        private async Task PrepareMessages(CancellationToken t)
        {
            while(true)
            {
                t.ThrowIfCancellationRequested();
                
                var nextEvent = _outputQueue.Take(t);

                IEvent @event = nextEvent.Item1;
                OutputMode outputMode = nextEvent.Item2;

                var buffer = _msgBufferPool.GetStream();
                await _serializer.Serialize(buffer, @event);

                switch (outputMode)
                {
                    case OutputMode.Broadcast:
                        foreach(var shardQueue in _shardedMessageQueues.Values)
                        {
                            shardQueue.Add(buffer);
                            //TODO: how to deal with situation:
                            // - enqueue memstream in 2 queues
                            // - first copies to network and disposes stream
                            // - second tries to copy --> ObjectDisposedException..
                        }
                        break;
                    case OutputMode.Partition:
                        throw new NotImplementedException("TODO: hash partitioning");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(outputMode));
                }

                //TODO: enqueue in correct/all shard queue
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

        public void Enqueue(IEvent @event, OutputMode mode)
        {
            _outputQueue.Add(new Tuple<IEvent, OutputMode>(@event, mode));
        }

        public void Enqueue(IEnumerable<IEvent> events, OutputMode mode)
        {
            foreach(var @event in events)
            {
                Enqueue(@event, mode);
            }
        }
    }
}
