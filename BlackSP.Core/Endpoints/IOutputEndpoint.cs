using BlackSP.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public interface IOutputEndpoint
    {

        /// <summary>
        /// Enqueue event in appropriate output queue
        /// applies partitioning function to determine 
        /// target output queue
        /// </summary>
        /// <param name="event"></param>
        void EnqueuePartitioned(IEvent @event);

        /// <summary>
        /// Enqueue event in every output queue
        /// useful for control messages or events 
        /// that need to be received by all shards 
        /// of an operator
        /// </summary>
        /// <param name="event"></param>
        void EnqueueAll(IEvent @event);

        /// <summary>
        /// Starts a blocking process that writes enqueued events to the outputstream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        void Egress(Stream outputStream, int remoteShardId, CancellationToken t);

        bool RegisterRemoteShard(int remoteShardId);

        bool UnregisterRemoteShard(int remoteShardId);
    }
}
