using Apex.Serialization;
using BlackSP.Core.Events;
using BlackSP.Core.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public class BaseOutputEndpoint : IOutputEndpoint
    {
        //default BlockingCollection implementation is a ConcurrentQueue..
        protected IDictionary<int, BlockingCollection<IEvent>> _outputQueues;
        protected int _shardCount;
        private IEventSerializer _serializer;

        public BaseOutputEndpoint()
        {
            Initialize(new ApexEventSerializer(Binary.Create()));
        }

        public BaseOutputEndpoint(IEventSerializer serializer)
        {
            Initialize(serializer);
        }

        private void Initialize(IEventSerializer serializer)
        {
            _shardCount = 0;
            _serializer = serializer;
            _outputQueues = new ConcurrentDictionary<int, BlockingCollection<IEvent>>();

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
            if(_outputQueues.ContainsKey(remoteShardId))
            {
                return false;
            }
            _outputQueues.Add(remoteShardId, new BlockingCollection<IEvent>());
            return true;
        }

        /// <summary>
        /// Unregisters a remote shard with given id
        /// </summary>
        /// <param name="remoteShardId"></param>
        /// <returns></returns>
        public bool UnregisterRemoteShard(int remoteShardId)
        {
            return _outputQueues.Remove(remoteShardId);
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
        public void Egress(Stream outputStream, int remoteShardId, CancellationToken t)
        {
            BlockingCollection<IEvent> blockingColl;
            if(!_outputQueues.TryGetValue(remoteShardId, out blockingColl))
            {
                throw new ArgumentException($"Remote shard with id {remoteShardId} has not been registered");
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double counter = 0;
            while(!t.IsCancellationRequested)
            {
                //TODO: consider error scenario where connection closes to not lose event
                IEvent @event = blockingColl.Take(); //TODO: consider changing ref back to normal even if that means valuecopy? as it already valuecopies now anyway
                _serializer.SerializeEvent(outputStream, ref @event);
                ;

                counter++;
                if (sw.ElapsedMilliseconds >= 10000) //every 10 seconds..
                {
                    double elapsedSeconds = (int)(sw.ElapsedMilliseconds / 1000d);
                    Console.WriteLine($"Egressing  {(int)(counter / elapsedSeconds)} e/s | {counter}/{elapsedSeconds}");
                    sw.Restart();
                    counter = 0;
                }
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
        /// Enqueue event in every output queue
        /// useful for control messages or events 
        /// that need to be received by all shards 
        /// of an operator
        /// </summary>
        /// <param name="event"></param>
        public void EnqueueAll(IEvent @event)
        {
            foreach(var blockingColl in _outputQueues.Values)
            {
                blockingColl.Add(@event);
            }
        }

        /// <summary>
        /// Enqueue event in appropriate output queue
        /// applies partitioning function to determine 
        /// target output queue
        /// </summary>
        /// <param name="event"></param>
        public void EnqueuePartitioned(IEvent @event)
        {
            throw new NotImplementedException("TODO (hash?) partitioning");
        }
    }
}
