using BlackSP.Interfaces.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Interfaces.Endpoints
{
    public enum OutputMode
    {
        Partition,
        Broadcast
    }

    public interface IOutputEndpoint
    {

        /// <summary>
        /// Enqueue event according to output mode
        /// </summary>
        /// <param name="event"></param>
        void Enqueue(IEvent @event, OutputMode mode);

        /// <summary>
        /// Enqueue events according to output mode
        /// </summary>
        /// <param name="events"></param>
        void Enqueue(IEnumerable<IEvent> events, OutputMode mode);

        /// <summary>
        /// Starts a blocking process that writes enqueued events to the outputstream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        Task Egress(Stream outputStream, int remoteShardId, CancellationToken t);

        bool RegisterRemoteShard(int remoteShardId);

        bool UnregisterRemoteShard(int remoteShardId);
    }
}
