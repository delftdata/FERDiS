using BlackSP.Kernel.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Endpoints
{
    public interface IOutputEndpoint
    {
        /// <summary>
        /// Starts a blocking process that writes enqueued events to the outputstream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        Task Egress(Stream outputStream, string remoteEndpointName, int remoteShardId, CancellationToken t);
    }
}
